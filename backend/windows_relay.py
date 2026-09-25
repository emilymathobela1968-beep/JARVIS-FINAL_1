"""Relay between the hosted JARVIS app and the local Windows companion agent.

The agent dials OUT to this backend over a WebSocket and authenticates with a
one-time pairing code, so no inbound port / tunnel is needed on the user's laptop.
Commands are routed agent-ward and results are awaited and logged.
"""
from __future__ import annotations

import asyncio
import logging
import secrets
import uuid
from dataclasses import dataclass, field
from datetime import datetime, timedelta, timezone
from typing import Any, Dict, Optional

from fastapi import APIRouter, HTTPException, WebSocket, WebSocketDisconnect
from pydantic import BaseModel

from windows_intents import CONFIRM_REQUIRED, parse_intent
from windows_compose import compose_email, compose_excel, compose_word

logger = logging.getLogger(__name__)

router = APIRouter(prefix="/windows")

PAIR_CODE_TTL = timedelta(minutes=15)
COMMAND_TIMEOUT = 180.0


@dataclass
class AgentSession:
    link_id: str
    ws: WebSocket
    hostname: str = "unknown"
    version: str = "unknown"
    capabilities: list = field(default_factory=list)
    connected_at: str = ""


# link_id -> live agent session
SESSIONS: Dict[str, AgentSession] = {}
# pairing code -> (link_id, expires_at)
PAIRING: Dict[str, tuple] = {}
# request_id -> future awaiting the agent's result
PENDING: Dict[str, asyncio.Future] = {}


def _now() -> str:
    return datetime.now(timezone.utc).isoformat()


class PairResponse(BaseModel):
    link_id: str
    code: str
    expires_at: str


class CommandRequest(BaseModel):
    link_id: str
    action: str
    params: Dict[str, Any] = {}
    confirmed: bool = False


class IntentRequest(BaseModel):
    link_id: Optional[str] = None
    text: str


def register_windows_routes(db):
    commands = db.windows_commands

    async def _log(entry: dict):
        await commands.insert_one({**entry})

    @router.post("/pair", response_model=PairResponse)
    async def create_pairing():
        link_id = str(uuid.uuid4())
        code = f"{secrets.randbelow(1_000_000) if hasattr(secrets, 'randbelow') else secrets.randbits(32) % 1_000_000:06d}"
        expires = datetime.now(timezone.utc) + PAIR_CODE_TTL
        PAIRING[code] = (link_id, expires)
        return PairResponse(link_id=link_id, code=code, expires_at=expires.isoformat())

    @router.get("/status")
    async def agent_status(link_id: Optional[str] = None):
        if link_id and link_id in SESSIONS:
            s = SESSIONS[link_id]
            return {
                "state": "connected",
                "link_id": link_id,
                "hostname": s.hostname,
                "version": s.version,
                "capabilities": s.capabilities,
                "connected_at": s.connected_at,
            }
        return {"state": "not_running", "link_id": link_id, "agents_online": len(SESSIONS)}

    @router.post("/intent")
    async def resolve_intent(req: IntentRequest):
        intent = parse_intent(req.text)
        if not intent:
            return {
                "understood": False,
                "message": "JARVIS could not map that to a Windows action yet.",
                "text": req.text,
            }
        return {
            "understood": True,
            "action": intent.action,
            "params": intent.params,
            "summary": intent.summary,
            "requires_confirmation": intent.requires_confirmation,
        }

    @router.post("/command")
    async def run_command(req: CommandRequest):
        if req.action in CONFIRM_REQUIRED and not req.confirmed:
            return {
                "status": "confirmation_required",
                "action": req.action,
                "message": f"'{req.action}' needs your confirmation before JARVIS executes it.",
            }

        session = SESSIONS.get(req.link_id)
        if not session:
            return {
                "status": "agent_offline",
                "message": "The local Windows agent is not connected. Start it on your Windows 11 machine.",
            }

        request_id = str(uuid.uuid4())
        params = dict(req.params or {})

        # Content for Office actions is composed server-side, never invented by the agent.
        try:
            instruction = params.get("instruction")
            if req.action == "office.word.create" and not params.get("body") and instruction:
                params.update(await compose_word(str(instruction), request_id))
            elif req.action == "office.excel.create" and not params.get("rows") and instruction:
                params.update(await compose_excel(str(instruction), request_id))
            elif req.action in ("outlook.draft", "outlook.send") and not params.get("body") and instruction:
                params.update(await compose_email(str(instruction), request_id))
        except Exception as e:  # noqa: BLE001
            logger.exception("Composition failed")
            return {"status": "failed", "action": req.action,
                    "error": f"Could not compose the content: {e}"}

        loop = asyncio.get_running_loop()
        future: asyncio.Future = loop.create_future()
        PENDING[request_id] = future

        record = {
            "id": request_id,
            "link_id": req.link_id,
            "action": req.action,
            "params": {k: v for k, v in params.items() if k != "body"},
            "status": "running",
            "created_at": _now(),
        }
        await _log(record)

        try:
            await session.ws.send_json(
                {"type": "command", "request_id": request_id, "action": req.action, "params": params}
            )
        except Exception as e:  # noqa: BLE001
            PENDING.pop(request_id, None)
            await commands.update_one({"id": request_id}, {"$set": {"status": "failed", "error": str(e)}})
            return {"status": "failed", "error": f"Could not reach the agent: {e}"}

        try:
            result = await asyncio.wait_for(future, timeout=COMMAND_TIMEOUT)
        except asyncio.TimeoutError:
            PENDING.pop(request_id, None)
            await commands.update_one(
                {"id": request_id}, {"$set": {"status": "failed", "error": "agent timeout"}}
            )
            return {"status": "failed", "error": "The agent did not answer in time."}

        ok = bool(result.get("ok"))
        await commands.update_one(
            {"id": request_id},
            {"$set": {
                "status": "completed" if ok else "failed",
                "data": result.get("data"),
                "error": result.get("error"),
                "finished_at": _now(),
            }},
        )
        return {
            "status": "completed" if ok else "failed",
            "id": request_id,
            "action": req.action,
            "data": result.get("data"),
            "error": result.get("error"),
        }

    @router.get("/log")
    async def command_log(link_id: Optional[str] = None, limit: int = 50):
        query = {"link_id": link_id} if link_id else {}
        rows = await commands.find(query, {"_id": 0}).sort("created_at", -1).to_list(limit)
        return {"commands": rows}

    @router.websocket("/agent")
    async def agent_socket(ws: WebSocket):
        await ws.accept()
        link_id: Optional[str] = None
        try:
            hello = await asyncio.wait_for(ws.receive_json(), timeout=20)
            if hello.get("type") != "hello":
                await ws.send_json({"type": "error", "message": "expected hello"})
                await ws.close()
                return

            code = str(hello.get("code", "")).strip()
            entry = PAIRING.get(code)
            if not entry or entry[1] < datetime.now(timezone.utc):
                PAIRING.pop(code, None)
                await ws.send_json({"type": "error", "message": "invalid or expired pairing code"})
                await ws.close()
                return

            link_id = entry[0]
            PAIRING.pop(code, None)
            SESSIONS[link_id] = AgentSession(
                link_id=link_id,
                ws=ws,
                hostname=str(hello.get("hostname", "unknown")),
                version=str(hello.get("version", "unknown")),
                capabilities=list(hello.get("capabilities", [])),
                connected_at=_now(),
            )
            await ws.send_json({"type": "ready", "link_id": link_id})
            logger.info("Windows agent paired: %s", link_id)

            while True:
                msg = await ws.receive_json()
                kind = msg.get("type")
                if kind == "result":
                    fut = PENDING.pop(msg.get("request_id", ""), None)
                    if fut and not fut.done():
                        fut.set_result(msg)
                elif kind == "ping":
                    await ws.send_json({"type": "pong"})
        except WebSocketDisconnect:
            pass
        except Exception as e:  # noqa: BLE001
            logger.warning("Agent socket error: %s", e)
        finally:
            if link_id:
                SESSIONS.pop(link_id, None)
                logger.info("Windows agent disconnected: %s", link_id)

    return router
