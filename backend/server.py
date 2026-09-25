from fastapi import FastAPI, APIRouter, HTTPException
from fastapi.responses import StreamingResponse
from dotenv import load_dotenv
from starlette.middleware.cors import CORSMiddleware
from motor.motor_asyncio import AsyncIOMotorClient
import os
import re
import json
import logging
from pathlib import Path
from pydantic import BaseModel, Field, ConfigDict
from typing import List, Optional
import uuid
from datetime import datetime, timezone

from emergentintegrations.llm.chat import LlmChat, UserMessage, TextDelta, StreamDone

from image_generation import register_image_routes
from windows_relay import register_windows_routes


ROOT_DIR = Path(__file__).parent
load_dotenv(ROOT_DIR / '.env')

# MongoDB connection
mongo_url = os.environ['MONGO_URL']
client = AsyncIOMotorClient(mongo_url)
db = client[os.environ['DB_NAME']]

EMERGENT_LLM_KEY = os.environ.get('EMERGENT_LLM_KEY')

# Create the main app without a prefix
app = FastAPI()

# Create a router with the /api prefix
api_router = APIRouter(prefix="/api")


# Define Models
class StatusCheck(BaseModel):
    model_config = ConfigDict(extra="ignore")  # Ignore MongoDB's _id field
    
    id: str = Field(default_factory=lambda: str(uuid.uuid4()))
    client_name: str
    timestamp: datetime = Field(default_factory=lambda: datetime.now(timezone.utc))

class StatusCheckCreate(BaseModel):
    client_name: str

# Add your routes to the router instead of directly to app
@api_router.get("/")
async def root():
    return {"message": "Hello World"}

@api_router.post("/status", response_model=StatusCheck)
async def create_status_check(input: StatusCheckCreate):
    status_dict = input.model_dump()
    status_obj = StatusCheck(**status_dict)
    
    # Convert to dict and serialize datetime to ISO string for MongoDB
    doc = status_obj.model_dump()
    doc['timestamp'] = doc['timestamp'].isoformat()
    
    _ = await db.status_checks.insert_one(doc)
    return status_obj

@api_router.get("/status", response_model=List[StatusCheck])
async def get_status_checks():
    # Exclude MongoDB's _id field from the query results
    status_checks = await db.status_checks.find({}, {"_id": 0}).to_list(1000)
    
    # Convert ISO string timestamps back to datetime objects
    for check in status_checks:
        if isinstance(check['timestamp'], str):
            check['timestamp'] = datetime.fromisoformat(check['timestamp'])
    
    return status_checks


# ---------------------------------------------------------------------------
# Builder — real web app generation (Milestone 1: Web App only)
# ---------------------------------------------------------------------------

SUPPORTED_APP_TYPES = {"web"}

WEB_SYSTEM_MESSAGE = (
    "You are JARVIS's web application generator. Given a user's objective, produce a SINGLE, "
    "complete, self-contained HTML5 document that fully implements the requested web app.\n\n"
    "HARD REQUIREMENTS:\n"
    "1. Output ONE HTML document beginning with <!DOCTYPE html>.\n"
    "2. Inline ALL CSS inside <style> tags and ALL JavaScript inside <script> tags.\n"
    "3. Do NOT reference any external files, CDNs, frameworks, fonts, images by URL, or any "
    "network resource. It must run fully offline inside a sandboxed iframe that only allows scripts.\n"
    "4. Build a polished, modern, responsive UI with real, working interactivity (buttons, inputs, "
    "state) implemented in vanilla JS. No placeholder 'coming soon' stubs — the core feature must work.\n"
    "5. Use only inline SVG or CSS for any graphics. Do not use <img> with external src.\n"
    "6. Do not fabricate real data, credentials, or claims of live connections. Sample/demo data is fine "
    "and should be clearly reasonable.\n\n"
    "OUTPUT FORMAT: return ONLY the raw HTML document. No markdown code fences, no commentary."
)


class GenerateRequest(BaseModel):
    objective: str
    app_type: Optional[str] = "web"
    session_id: Optional[str] = None


def _extract_html(text: str) -> str:
    """Strip markdown fences / stray prose and return the HTML document."""
    t = text.strip()
    # Remove ```html ... ``` or ``` ... ``` fences if the model added them.
    fence = re.match(r"^```(?:html)?\s*(.*?)\s*```$", t, re.DOTALL | re.IGNORECASE)
    if fence:
        t = fence.group(1).strip()
    # If there is leading prose before the doctype/html, cut to the first tag.
    lower = t.lower()
    for marker in ("<!doctype html", "<html"):
        idx = lower.find(marker)
        if idx != -1:
            t = t[idx:]
            break
    return t.strip()


@api_router.post("/builder/generate")
async def builder_generate(req: GenerateRequest):
    objective = (req.objective or "").strip()
    app_type = (req.app_type or "web").strip().lower()

    if not objective:
        raise HTTPException(status_code=400, detail="objective is required")

    if app_type not in SUPPORTED_APP_TYPES:
        # Honest unavailable state — surfaced as a 422 the frontend renders as BLOCKED.
        raise HTTPException(
            status_code=422,
            detail=f"'{app_type}' generation is not available in this milestone. Only Web App is supported.",
        )

    if not EMERGENT_LLM_KEY:
        raise HTTPException(status_code=503, detail="LLM key not configured on the server.")

    generation_id = str(uuid.uuid4())
    session_id = req.session_id or generation_id

    doc = {
        "id": generation_id,
        "objective": objective,
        "app_type": app_type,
        "session_id": session_id,
        "status": "generating",
        "source": None,
        "error": None,
        "created_at": datetime.now(timezone.utc).isoformat(),
    }
    await db.generations.insert_one({**doc})

    def sse(event: str, data: dict) -> str:
        return f"event: {event}\ndata: {json.dumps(data)}\n\n"

    async def event_generator():
        yield sse("start", {"id": generation_id, "session_id": session_id})
        chat = LlmChat(
            api_key=EMERGENT_LLM_KEY,
            session_id=session_id,
            system_message=WEB_SYSTEM_MESSAGE,
        ).with_model("openai", "gpt-5.4")

        full = ""
        try:
            async for ev in chat.stream_message(UserMessage(text=objective)):
                if isinstance(ev, TextDelta):
                    full += ev.content
                    yield sse("delta", {"content": ev.content})
                elif isinstance(ev, StreamDone):
                    break

            source = _extract_html(full)
            if not source or "<" not in source:
                await db.generations.update_one(
                    {"id": generation_id},
                    {"$set": {"status": "failed", "error": "Model returned no renderable HTML."}},
                )
                yield sse("error", {"id": generation_id, "message": "Model returned no renderable HTML.",
                                    "evidence": f"chars_received={len(full)}"})
                return

            await db.generations.update_one(
                {"id": generation_id},
                {"$set": {"status": "unverified", "source": source}},
            )
            yield sse("done", {"id": generation_id, "source": source,
                               "evidence": f"bytes={len(source.encode('utf-8'))}"})
        except Exception as e:  # noqa: BLE001
            logger.exception("Generation failed")
            await db.generations.update_one(
                {"id": generation_id},
                {"$set": {"status": "failed", "error": str(e)}},
            )
            yield sse("error", {"id": generation_id, "message": "Generation run failed.",
                                "evidence": str(e)[:500]})

    return StreamingResponse(
        event_generator(),
        media_type="text/event-stream",
        headers={"Cache-Control": "no-cache", "X-Accel-Buffering": "no", "Connection": "keep-alive"},
    )


@api_router.post("/builder/verify/{generation_id}")
async def builder_verify(generation_id: str):
    """Record that a generated artifact rendered and was interacted with in the sandbox."""
    gen = await db.generations.find_one({"id": generation_id}, {"_id": 0})
    if not gen:
        raise HTTPException(status_code=404, detail="generation not found")
    await db.generations.update_one(
        {"id": generation_id},
        {"$set": {"status": "verified", "verified_at": datetime.now(timezone.utc).isoformat()}},
    )
    return {"id": generation_id, "status": "verified"}

# Include the router in the main app
api_router.include_router(register_image_routes(db))
api_router.include_router(register_windows_routes(db))
app.include_router(api_router)

app.add_middleware(
    CORSMiddleware,
    allow_credentials=True,
    allow_origins=os.environ.get('CORS_ORIGINS', '*').split(','),
    allow_methods=["*"],
    allow_headers=["*"],
)

# Configure logging
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(name)s - %(levelname)s - %(message)s'
)
logger = logging.getLogger(__name__)

@app.on_event("shutdown")
async def shutdown_db_client():
    client.close()