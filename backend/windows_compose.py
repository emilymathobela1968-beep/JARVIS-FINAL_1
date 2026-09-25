"""Server-side content composition for Windows Office actions.

The agent stays 'dumb' — it only types what it is given. Document, spreadsheet and
email content is composed here with the LLM so nothing is fabricated on the client.
"""
from __future__ import annotations

import json
import os
import re
from datetime import date
from typing import Any, Dict

from emergentintegrations.llm.chat import LlmChat, UserMessage

MODEL = ("openai", "gpt-5.4")


def _key() -> str | None:
    return os.environ.get("EMERGENT_LLM_KEY")


async def _ask(system: str, prompt: str, session: str) -> str:
    key = _key()
    if not key:
        raise RuntimeError("LLM key is not configured on the server.")
    chat = LlmChat(api_key=key, session_id=session, system_message=system).with_model(*MODEL)
    return await chat.send_message(UserMessage(text=prompt))


DOC_SYSTEM = (
    "You write finished business documents for a Windows automation agent. "
    "Return ONLY the document text, ready to be typed into Microsoft Word. "
    "No markdown, no code fences, no commentary, no placeholders in square brackets "
    "unless the user genuinely left a detail out. Use blank lines between paragraphs. "
    f"Today's date is {date.today().isoformat()}."
)

SHEET_SYSTEM = (
    "You produce spreadsheet data for a Windows automation agent. "
    "Return ONLY a JSON object: {\"sheet_name\": string, \"rows\": [[cell, cell, ...], ...]} "
    "where the first row is the header. No markdown, no commentary."
)

EMAIL_SYSTEM = (
    "You write business emails for a Windows automation agent. "
    "Return ONLY a JSON object: {\"subject\": string, \"body\": string}. "
    "The body must be a complete, ready-to-send email with a greeting and sign-off. "
    "No markdown, no commentary."
)


def _first_json(text: str) -> Dict[str, Any]:
    t = text.strip()
    fence = re.match(r"^```(?:json)?\s*(.*?)\s*```$", t, re.DOTALL)
    if fence:
        t = fence.group(1)
    start = t.find("{")
    end = t.rfind("}")
    if start == -1 or end == -1:
        raise ValueError("model did not return JSON")
    return json.loads(t[start : end + 1])


async def compose_word(instruction: str, session: str) -> Dict[str, Any]:
    body = await _ask(DOC_SYSTEM, instruction, session)
    body = re.sub(r"^```.*?\n|```$", "", body.strip(), flags=re.DOTALL).strip()
    return {"body": body}


async def compose_excel(instruction: str, session: str) -> Dict[str, Any]:
    raw = await _ask(SHEET_SYSTEM, instruction, session)
    data = _first_json(raw)
    rows = data.get("rows") or []
    if not rows:
        raise ValueError("model returned no rows")
    return {"rows": rows, "sheet_name": str(data.get("sheet_name") or "JARVIS")[:31]}


async def compose_email(instruction: str, session: str) -> Dict[str, Any]:
    raw = await _ask(EMAIL_SYSTEM, instruction, session)
    data = _first_json(raw)
    return {"subject": str(data.get("subject") or "").strip(),
            "body": str(data.get("body") or "").strip()}
