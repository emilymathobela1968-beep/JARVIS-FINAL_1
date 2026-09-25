"""JARVIS native image generation (Gemini / Nano Banana via the Emergent universal key).

The provider key lives ONLY in backend/.env. Nothing about the key is ever returned to the
client. Images are written to disk and served back through an /api route.
"""
from __future__ import annotations

import base64
import logging
import os
import uuid
from datetime import datetime, timezone
from pathlib import Path
from typing import Literal, Optional

from fastapi import APIRouter, HTTPException
from fastapi.responses import FileResponse
from pydantic import BaseModel

from emergentintegrations.llm.chat import LlmChat, UserMessage

logger = logging.getLogger(__name__)

router = APIRouter(prefix="/image-generation")

IMAGES_DIR = Path(__file__).parent / "generated_images"
IMAGES_DIR.mkdir(exist_ok=True)

PROVIDER = os.environ.get("IMAGE_GENERATION_PROVIDER", "gemini").strip().lower()
MODEL = os.environ.get("IMAGE_GENERATION_MODEL", "gemini-3.1-flash-image-preview").strip()


def _api_key() -> Optional[str]:
    return os.environ.get("IMAGE_GENERATION_API_KEY") or os.environ.get("EMERGENT_LLM_KEY")


Style = Literal["realistic", "cinematic", "product", "social_post", "ui_mockup", "logo", "freeform"]
Ratio = Literal["1:1", "16:9", "9:16", "4:5"]
Quality = Literal["standard", "high"]

STYLE_DIRECTION = {
    "realistic": "photorealistic, natural lighting, true-to-life detail and materials",
    "cinematic": "cinematic film still, dramatic lighting, shallow depth of field, colour graded",
    "product": "clean studio product photography, soft box lighting, seamless backdrop",
    "social_post": "bold social-media graphic, strong focal subject, room for a headline",
    "ui_mockup": "clean digital UI mockup, crisp typography, precise alignment, flat modern interface",
    "logo": "minimal vector-style logo mark, flat solid shapes, centred, plain background",
    "freeform": "",
}


class GenerateImageRequest(BaseModel):
    prompt: str
    style: Style = "realistic"
    aspect_ratio: Ratio = "1:1"
    quality: Quality = "standard"


class GenerateImageResponse(BaseModel):
    id: str
    status: Literal["generating", "completed", "failed", "blocked"]
    image_url: Optional[str] = None
    error: Optional[str] = None
    prompt: Optional[str] = None
    style: Optional[str] = None
    aspect_ratio: Optional[str] = None


def _build_prompt(req: GenerateImageRequest) -> str:
    parts = [req.prompt.strip()]
    direction = STYLE_DIRECTION.get(req.style, "")
    if direction:
        parts.append(f"Style: {direction}.")
    parts.append(f"Compose the image with a strict {req.aspect_ratio} aspect ratio, filling the frame.")
    if req.quality == "high":
        parts.append("Render with maximum detail and sharpness.")
    parts.append("Do not include any watermark, caption or added text unless the prompt asks for text.")
    return " ".join(parts)


def register_image_routes(db):
    collection = db.image_generations

    @router.post("/generate", response_model=GenerateImageResponse)
    async def generate_image(req: GenerateImageRequest):
        prompt = (req.prompt or "").strip()
        if not prompt:
            raise HTTPException(status_code=400, detail="prompt is required")

        key = _api_key()
        if not key:
            # Honest blocked state — no placeholder image is ever returned.
            return GenerateImageResponse(
                id="",
                status="blocked",
                error="Image generation provider is not connected.",
            )

        image_id = str(uuid.uuid4())
        record = {
            "id": image_id,
            "prompt": prompt,
            "style": req.style,
            "aspect_ratio": req.aspect_ratio,
            "quality": req.quality,
            "provider": PROVIDER,
            "model": MODEL,
            "status": "generating",
            "error": None,
            "created_at": datetime.now(timezone.utc).isoformat(),
        }
        await collection.insert_one({**record})

        try:
            images = []
            text = ""
            # One retry: the model occasionally answers with text only.
            for attempt in range(2):
                chat = LlmChat(
                    api_key=key,
                    session_id=f"image-{image_id}-{attempt}",
                    system_message=(
                        "You are JARVIS's image generation engine. Always respond with a generated image."
                    ),
                ).with_model("gemini", MODEL).with_params(modalities=["image", "text"])

                text, images = await chat.send_message_multimodal_response(
                    UserMessage(text=_build_prompt(req))
                )
                if images:
                    break
                logger.warning("Image attempt %s returned no image", attempt + 1)

            if not images:
                reason = (text or "Provider returned no image.").strip()[:400]
                await collection.update_one(
                    {"id": image_id}, {"$set": {"status": "failed", "error": reason}}
                )
                return GenerateImageResponse(id=image_id, status="failed", error=reason)

            img = images[0]
            data = base64.b64decode(img["data"])
            mime = img.get("mime_type", "image/png")
            ext = "jpg" if "jpeg" in mime else "png"
            path = IMAGES_DIR / f"{image_id}.{ext}"
            path.write_bytes(data)

            await collection.update_one(
                {"id": image_id},
                {"$set": {"status": "completed", "file": path.name, "mime_type": mime,
                          "bytes": len(data)}},
            )
            return GenerateImageResponse(
                id=image_id,
                status="completed",
                image_url=f"/api/image-generation/image/{image_id}",
                prompt=prompt,
                style=req.style,
                aspect_ratio=req.aspect_ratio,
            )
        except Exception as e:  # noqa: BLE001
            logger.exception("Image generation failed")
            message = str(e)[:400] or "Image generation failed."
            await collection.update_one(
                {"id": image_id}, {"$set": {"status": "failed", "error": message}}
            )
            return GenerateImageResponse(id=image_id, status="failed", error=message)

    @router.get("/status", response_model=dict)
    async def provider_status():
        return {"provider": PROVIDER, "model": MODEL, "connected": bool(_api_key())}

    @router.get("/image/{image_id}")
    async def get_image(image_id: str):
        record = await collection.find_one({"id": image_id}, {"_id": 0})
        if not record or record.get("status") != "completed":
            raise HTTPException(status_code=404, detail="image not found")
        path = IMAGES_DIR / record["file"]
        if not path.exists():
            raise HTTPException(status_code=404, detail="image file missing")
        return FileResponse(path, media_type=record.get("mime_type", "image/png"))

    @router.get("/{image_id}", response_model=GenerateImageResponse)
    async def get_generation(image_id: str):
        record = await collection.find_one({"id": image_id}, {"_id": 0})
        if not record:
            raise HTTPException(status_code=404, detail="generation not found")
        return GenerateImageResponse(
            id=record["id"],
            status=record["status"],
            image_url=(
                f"/api/image-generation/image/{image_id}" if record["status"] == "completed" else None
            ),
            error=record.get("error"),
            prompt=record.get("prompt"),
            style=record.get("style"),
            aspect_ratio=record.get("aspect_ratio"),
        )

    return router
