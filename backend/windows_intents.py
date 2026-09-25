"""Natural-language → Windows action intent layer.

Pure functions only, so the routing can be unit-tested on any OS (including this
Linux build container). The agent executes; this module only decides WHAT to run.
"""
from __future__ import annotations

import re
from dataclasses import dataclass, field
from typing import Any, Dict, Optional

# Actions that must never run without an explicit user confirmation.
CONFIRM_REQUIRED = {
    "outlook.send",
    "shell.run",
    "file.delete",
    "system.setting_change",
    "app.install",
}


@dataclass
class Intent:
    action: str
    params: Dict[str, Any] = field(default_factory=dict)
    summary: str = ""
    requires_confirmation: bool = False


APP_ALIASES = {
    "word": "winword",
    "microsoft word": "winword",
    "ms word": "winword",
    "excel": "excel",
    "microsoft excel": "excel",
    "powerpoint": "powerpnt",
    "outlook": "outlook",
    "calculator": "calculator",
    "calc": "calculator",
    "notepad": "notepad",
    "wordpad": "wordpad",
    "paint": "mspaint",
    "file explorer": "explorer",
    "explorer": "explorer",
    "browser": "browser",
    "edge": "msedge",
    "chrome": "chrome",
    "command prompt": "cmd",
    "cmd": "cmd",
    "powershell": "powershell",
    "terminal": "wt",
    "task manager": "taskmgr",
    "control panel": "control_panel",
    "device manager": "device_manager",
    "settings": "settings",
}

SYSTEM_TARGETS = {
    "control panel": "control_panel",
    "device manager": "device_manager",
    "network settings": "network",
    "network connections": "network",
    "network": "network",
    "wifi settings": "network",
    "bluetooth": "bluetooth",
    "bluetooth settings": "bluetooth",
    "sound settings": "sound",
    "sound": "sound",
    "display settings": "display",
    "display": "display",
    "programs and features": "programs_features",
    "installed programs": "programs_features",
    "task manager": "task_manager",
    "windows settings": "settings",
    "settings": "settings",
}

INFO_TARGETS = [
    (r"\b(date|time|clock)\b", "time"),
    (r"\bbatter(y|ies)\b", "battery"),
    (r"\b(disk|storage|drive space|disk space)\b", "disk"),
    (r"\b(network status|internet|ip address|connection status)\b", "network"),
    (r"\b(running processes|processes|task list)\b", "processes"),
    (r"\b(installed apps|installed programs|installed software)\b", "installed_apps"),
]


def _match_known(text: str, table: Dict[str, str]) -> Optional[str]:
    """Longest alias wins so 'microsoft word' beats 'word'."""
    best: Optional[str] = None
    best_len = 0
    for alias, value in table.items():
        if re.search(rf"\b{re.escape(alias)}\b", text) and len(alias) > best_len:
            best, best_len = value, len(alias)
    return best


def _clean(text: str) -> str:
    return re.sub(r"\s+", " ", (text or "").strip().lower())


def _intent(action: str, params: Dict[str, Any], summary: str) -> Intent:
    return Intent(
        action=action,
        params=params,
        summary=summary,
        requires_confirmation=action in CONFIRM_REQUIRED,
    )


def parse_intent(raw: str) -> Optional[Intent]:
    text = _clean(raw)
    if not text:
        return None

    # --- Email --------------------------------------------------------------
    if re.search(r"\b(check|read|show|any new)\b.*\b(email|emails|inbox|mail)\b", text):
        return _intent("outlook.inbox_summary", {"count": 10}, "Read the latest inbox summary")

    if re.search(r"\bsearch\b.*\b(email|emails|mail|inbox)\b", text):
        q = re.sub(r".*\b(?:for|about)\b", "", text).strip() or text
        return _intent("outlook.search", {"query": q}, f"Search email for '{q}'")

    if re.search(r"\b(send|write|draft|compose|reply to)\b.*\b(email|mail)\b", text):
        to = None
        m = re.search(r"\bto\s+([a-z0-9._%+-]+@[a-z0-9.-]+\.[a-z]{2,})", text)
        if m:
            to = m.group(1)
        else:
            m = re.search(r"\bto\s+([a-z][a-z\s'.-]{1,40}?)(?:\s+(?:about|regarding|saying|with)\b|$)", text)
            if m:
                to = m.group(1).strip()
        subject = None
        m = re.search(r"\b(?:about|regarding|subject)\s+(.+)$", text)
        if m:
            subject = m.group(1).strip()
        # Drafting never sends; sending is a separate confirmed action.
        return _intent(
            "outlook.draft",
            {"to": to, "subject": subject, "body": None, "instruction": raw.strip()},
            f"Draft an email{f' to {to}' if to else ''} (not sent)",
        )

    # --- Word / Excel documents --------------------------------------------
    if re.search(r"\b(write|draft|create|make|type)\b.*\b(letter|resignation|document|doc|memo|report|cv|resume)\b", text):
        return _intent(
            "office.word.create",
            {"instruction": raw.strip()},
            "Create a Word document from your instruction",
        )

    if re.search(r"\b(create|make|build)\b.*\b(spreadsheet|workbook|excel sheet|table)\b", text):
        return _intent(
            "office.excel.create",
            {"instruction": raw.strip()},
            "Create an Excel workbook from your instruction",
        )

    # --- System navigation --------------------------------------------------
    if re.search(r"\b(open|go to|show|launch|take me to)\b", text):
        target = _match_known(text, SYSTEM_TARGETS)
        if target:
            return _intent("windows.open", {"target": target}, f"Open {target.replace('_', ' ')}")

        app = _match_known(text, APP_ALIASES)
        if app:
            return _intent("app.launch", {"app": app}, f"Launch {app}")

        # "open <anything else>" -> best-effort Start menu lookup
        m = re.search(r"\b(?:open|launch|start)\s+(.+)$", text)
        if m:
            name = m.group(1).strip()
            return _intent("app.launch", {"app": name}, f"Launch '{name}' via Start menu lookup")

    # --- System information -------------------------------------------------
    if re.search(r"\b(check|what(?:'s| is)|show|how much|tell me)\b", text):
        for pattern, what in INFO_TARGETS:
            if re.search(pattern, text):
                return _intent("system.info", {"what": what}, f"Read {what.replace('_', ' ')}")

    # Bare "open word" style with no verb match but a known app
    app = _match_known(text, APP_ALIASES)
    if app:
        return _intent("app.launch", {"app": app}, f"Launch {app}")

    for pattern, what in INFO_TARGETS:
        if re.search(pattern, text):
            return _intent("system.info", {"what": what}, f"Read {what.replace('_', ' ')}")

    return None
