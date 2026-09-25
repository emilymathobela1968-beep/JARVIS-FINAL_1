"""Local persistent memory for the JARVIS Windows agent.

Plain JSON on disk next to the agent. Never stores passwords or secrets — only
app paths, contacts, preferences, recent tasks and successful/failed command routes.
"""
from __future__ import annotations

import json
import threading
from datetime import datetime, timezone
from pathlib import Path
from typing import Any, Dict

DEFAULT_PATH = Path(__file__).parent / "jarvis_memory.json"

SECRET_HINTS = ("password", "passwd", "secret", "token", "apikey", "api_key", "credential")


class MemoryStore:
    def __init__(self, path: Path | None = None):
        self.path = path or DEFAULT_PATH
        self._lock = threading.Lock()
        self._data: Dict[str, Any] = {
            "app_paths": {},
            "contacts": {},
            "preferences": {},
            "recent_tasks": [],
            "documents": [],
            "failed_routes": {},
        }
        self._load()

    def _load(self):
        if self.path.exists():
            try:
                loaded = json.loads(self.path.read_text(encoding="utf-8"))
                if isinstance(loaded, dict):
                    self._data.update(loaded)
            except Exception:  # noqa: BLE001
                pass

    def _save(self):
        self.path.write_text(json.dumps(self._data, indent=2), encoding="utf-8")

    # -- generic key/value ---------------------------------------------------
    def get(self, key: str) -> Any:
        with self._lock:
            return self._data.get("preferences", {}).get(key)

    def set(self, key: str, value: Any) -> bool:
        if any(h in key.lower() for h in SECRET_HINTS):
            raise ValueError("Refusing to store credentials in JARVIS memory.")
        with self._lock:
            self._data.setdefault("preferences", {})[key] = value
            self._save()
        return True

    def dump(self) -> Dict[str, Any]:
        with self._lock:
            return json.loads(json.dumps(self._data))

    # -- app routes ----------------------------------------------------------
    def get_app_path(self, app: str) -> str | None:
        with self._lock:
            return self._data.get("app_paths", {}).get(app)

    def remember_app_path(self, app: str, path: str):
        with self._lock:
            self._data.setdefault("app_paths", {})[app] = path
            self._save()

    def forget_app_path(self, app: str):
        with self._lock:
            self._data.get("app_paths", {}).pop(app, None)
            self._save()

    def remember_failure(self, app: str, error: str):
        with self._lock:
            self._data.setdefault("failed_routes", {})[app] = {
                "error": error[:300],
                "at": datetime.now(timezone.utc).isoformat(),
            }
            self._save()

    # -- contacts / documents / tasks ---------------------------------------
    def remember_contact(self, name: str, email: str):
        with self._lock:
            self._data.setdefault("contacts", {})[name.lower()] = email
            self._save()

    def lookup_contact(self, name: str) -> str | None:
        with self._lock:
            return self._data.get("contacts", {}).get(name.lower())

    def remember_document(self, path: str, summary: str):
        with self._lock:
            self._data.setdefault("documents", []).insert(
                0, {"path": path, "summary": summary[:200],
                    "at": datetime.now(timezone.utc).isoformat()}
            )
            self._data["documents"] = self._data["documents"][:50]
            self._save()

    def remember_task(self, action: str, ok: bool, detail: str = ""):
        with self._lock:
            self._data.setdefault("recent_tasks", []).insert(
                0, {"action": action, "ok": ok, "detail": detail[:200],
                    "at": datetime.now(timezone.utc).isoformat()}
            )
            self._data["recent_tasks"] = self._data["recent_tasks"][:100]
            self._save()
