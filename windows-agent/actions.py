"""JARVIS Windows companion agent — action registry.

Every action is a small, explicit function. Windows-only imports are done lazily so
this module can be imported (and unit-tested) on Linux/macOS build machines.
"""
from __future__ import annotations

import os
import platform
import shutil
import subprocess
import sys
from datetime import datetime
from typing import Any, Callable, Dict

from memory_store import MemoryStore

IS_WINDOWS = sys.platform == "win32"

# Executable / shell targets for well-known apps.
APP_TARGETS: Dict[str, str] = {
    "winword": "winword.exe",
    "excel": "excel.exe",
    "powerpnt": "powerpnt.exe",
    "outlook": "outlook.exe",
    "calculator": "calc.exe",
    "notepad": "notepad.exe",
    "wordpad": "write.exe",
    "mspaint": "mspaint.exe",
    "explorer": "explorer.exe",
    "msedge": "msedge.exe",
    "chrome": "chrome.exe",
    "cmd": "cmd.exe",
    "powershell": "powershell.exe",
    "wt": "wt.exe",
    "taskmgr": "taskmgr.exe",
    "browser": "msedge.exe",
}

# Shell / control-panel style targets.
SYSTEM_TARGETS: Dict[str, str] = {
    "control_panel": "control",
    "device_manager": "devmgmt.msc",
    "task_manager": "taskmgr.exe",
    "programs_features": "appwiz.cpl",
    "network": "ms-settings:network-status",
    "bluetooth": "ms-settings:bluetooth",
    "sound": "ms-settings:sound",
    "display": "ms-settings:display",
    "settings": "ms-settings:",
    "file_explorer": "explorer.exe",
}

memory = MemoryStore()


class ActionError(Exception):
    pass


def _require_windows():
    if not IS_WINDOWS:
        raise ActionError("This action requires Windows 11 (agent is running on a non-Windows host).")


def _start(target: str, args: list[str] | None = None) -> str:
    """Launch via ShellExecute semantics so protocols/.msc/.cpl/.lnk all work."""
    _require_windows()
    args = args or []
    lower = target.lower()
    shell_like = (
        lower.endswith((".msc", ".cpl", ".lnk", ".url"))
        or lower.startswith("shell:")
        or ("://" in lower)
        or (":" in lower and "\\" not in lower and "/" not in lower and not lower.endswith(".exe"))
    )
    if shell_like:
        if lower.startswith("shell:"):
            subprocess.Popen(["explorer.exe", target], close_fds=True)
        else:
            os.startfile(target)  # type: ignore[attr-defined]
        return f"shell:{target}"
    exe = shutil.which(target) or target
    subprocess.Popen([exe, *args], close_fds=True)
    return exe


# ---------------------------------------------------------------------------
# Apps
# ---------------------------------------------------------------------------

def app_launch(params: Dict[str, Any]) -> Dict[str, Any]:
    name = str(params.get("app", "")).strip()
    if not name:
        raise ActionError("app is required")

    key = name.lower()
    # 1. remembered route
    remembered = memory.get_app_path(key)
    if remembered:
        try:
            _start(remembered)
            return {"launched": name, "route": "memory", "target": remembered}
        except Exception:  # noqa: BLE001
            memory.forget_app_path(key)

    # 2. known target
    target = APP_TARGETS.get(key) or SYSTEM_TARGETS.get(key)
    if target:
        used = _start(target)
        memory.remember_app_path(key, target)
        return {"launched": name, "route": "known_target", "target": used}

    # 3. Start menu / AppsFolder lookup
    _require_windows()
    match = _find_start_menu_app(name)
    if match:
        os.startfile(match["path"])  # type: ignore[attr-defined]
        memory.remember_app_path(key, match["path"])
        return {"launched": match["name"], "route": "start_menu", "target": match["path"]}

    aumid = _find_appsfolder_app(name)
    if aumid:
        subprocess.Popen(["explorer.exe", f"shell:AppsFolder\\{aumid}"], close_fds=True)
        memory.remember_app_path(key, f"shell:AppsFolder\\{aumid}")
        return {"launched": name, "route": "appsfolder", "target": aumid}

    # 4. last resort: let Windows resolve it
    try:
        os.startfile(name)  # type: ignore[attr-defined]
        return {"launched": name, "route": "shell_execute", "target": name}
    except Exception as e:  # noqa: BLE001
        memory.remember_failure(key, str(e))
        raise ActionError(f"Could not find an app called '{name}'.") from e


def _start_menu_dirs() -> list[str]:
    dirs = []
    for env in ("APPDATA", "PROGRAMDATA"):
        base = os.environ.get(env)
        if base:
            dirs.append(os.path.join(base, "Microsoft", "Windows", "Start Menu", "Programs"))
    return [d for d in dirs if os.path.isdir(d)]


def _find_start_menu_app(name: str) -> Dict[str, str] | None:
    needle = name.lower()
    best = None
    for root_dir in _start_menu_dirs():
        for root, _dirs, files in os.walk(root_dir):
            for f in files:
                if not f.lower().endswith((".lnk", ".url")):
                    continue
                stem = os.path.splitext(f)[0]
                low = stem.lower()
                if needle == low:
                    return {"name": stem, "path": os.path.join(root, f)}
                if needle in low and (best is None or len(low) < len(best["name"])):
                    best = {"name": stem, "path": os.path.join(root, f)}
    return best


def _find_appsfolder_app(name: str) -> str | None:
    """Resolve an AUMID through PowerShell's Get-StartApps."""
    try:
        out = subprocess.run(
            [
                "powershell.exe",
                "-NoProfile",
                "-Command",
                f"Get-StartApps | Where-Object {{ $_.Name -like '*{name}*' }} | "
                "Select-Object -First 1 -ExpandProperty AppID",
            ],
            capture_output=True,
            text=True,
            timeout=25,
        )
        value = (out.stdout or "").strip()
        return value or None
    except Exception:  # noqa: BLE001
        return None


def app_list_installed(params: Dict[str, Any]) -> Dict[str, Any]:
    _require_windows()
    out = subprocess.run(
        ["powershell.exe", "-NoProfile", "-Command",
         "Get-StartApps | Select-Object -ExpandProperty Name"],
        capture_output=True, text=True, timeout=60,
    )
    apps = [line.strip() for line in (out.stdout or "").splitlines() if line.strip()]
    return {"count": len(apps), "apps": apps[: int(params.get("limit", 200))]}


# ---------------------------------------------------------------------------
# Windows navigation
# ---------------------------------------------------------------------------

def windows_open(params: Dict[str, Any]) -> Dict[str, Any]:
    target_key = str(params.get("target", "")).strip().lower()
    target = SYSTEM_TARGETS.get(target_key)
    if not target:
        raise ActionError(f"Unknown Windows target '{target_key}'.")
    used = _start(target)
    return {"opened": target_key, "target": used}


# ---------------------------------------------------------------------------
# System information
# ---------------------------------------------------------------------------

def system_info(params: Dict[str, Any]) -> Dict[str, Any]:
    what = str(params.get("what", "time")).strip().lower()

    if what == "time":
        now = datetime.now()
        return {"local_time": now.strftime("%H:%M:%S"), "date": now.strftime("%Y-%m-%d"),
                "timezone": str(now.astimezone().tzinfo)}

    if what == "disk":
        disks = []
        if IS_WINDOWS:
            import string
            for letter in string.ascii_uppercase:
                root = f"{letter}:\\"
                if os.path.exists(root):
                    total, used, free = shutil.disk_usage(root)
                    disks.append({"drive": root, "total_gb": round(total / 1e9, 1),
                                  "used_gb": round(used / 1e9, 1), "free_gb": round(free / 1e9, 1)})
        else:
            total, used, free = shutil.disk_usage("/")
            disks.append({"drive": "/", "total_gb": round(total / 1e9, 1),
                          "used_gb": round(used / 1e9, 1), "free_gb": round(free / 1e9, 1)})
        return {"disks": disks}

    if what == "battery":
        try:
            import psutil  # type: ignore
            b = psutil.sensors_battery()
            if not b:
                return {"battery": None, "note": "No battery reported by this machine."}
            return {"percent": b.percent, "plugged_in": b.power_plugged,
                    "seconds_left": None if b.secsleft < 0 else b.secsleft}
        except Exception as e:  # noqa: BLE001
            raise ActionError(f"Battery status unavailable: {e}") from e

    if what == "processes":
        try:
            import psutil  # type: ignore
            rows = []
            for p in psutil.process_iter(["pid", "name", "memory_info"]):
                info = p.info
                rows.append({"pid": info["pid"], "name": info["name"],
                             "memory_mb": round((info["memory_info"].rss if info["memory_info"] else 0) / 1e6, 1)})
            rows.sort(key=lambda r: r["memory_mb"], reverse=True)
            return {"count": len(rows), "top": rows[: int(params.get("limit", 15))]}
        except Exception as e:  # noqa: BLE001
            raise ActionError(f"Process list unavailable: {e}") from e

    if what == "network":
        host = platform.node()
        if IS_WINDOWS:
            out = subprocess.run(["powershell.exe", "-NoProfile", "-Command",
                                  "Get-NetConnectionProfile | Select-Object Name,IPv4Connectivity | Format-List"],
                                 capture_output=True, text=True, timeout=30)
            return {"hostname": host, "detail": (out.stdout or "").strip()[:2000]}
        return {"hostname": host, "detail": "Non-Windows host; limited network detail."}

    if what == "installed_apps":
        return app_list_installed(params)

    raise ActionError(f"Unknown info target '{what}'.")


# ---------------------------------------------------------------------------
# UI Automation fallback
# ---------------------------------------------------------------------------

def _uia():
    _require_windows()
    try:
        import uiautomation as auto  # type: ignore
        return auto
    except ImportError as e:  # noqa: BLE001
        raise ActionError("uiautomation is not installed in the agent environment.") from e


def ui_foreground(params: Dict[str, Any]) -> Dict[str, Any]:
    auto = _uia()
    title = str(params.get("title", "")).strip()
    win = auto.WindowControl(searchDepth=1, SubName=title)
    if not win.Exists(3, 1):
        raise ActionError(f"No window matching '{title}'.")
    win.SetActive()
    win.SetTopmost(False)
    return {"foreground": win.Name}


def ui_type(params: Dict[str, Any]) -> Dict[str, Any]:
    auto = _uia()
    text = str(params.get("text", ""))
    title = str(params.get("title", "")).strip()
    if title:
        ui_foreground({"title": title})
    auto.SendKeys(text.replace("\n", "{Enter}"), waitTime=0.02)
    return {"typed_chars": len(text), "window": title or "active window"}


def ui_click(params: Dict[str, Any]) -> Dict[str, Any]:
    auto = _uia()
    title = str(params.get("title", "")).strip()
    name = str(params.get("name", "")).strip()
    root = auto.WindowControl(searchDepth=1, SubName=title) if title else auto.GetRootControl()
    if title and not root.Exists(3, 1):
        raise ActionError(f"No window matching '{title}'.")
    ctrl = root.ButtonControl(SubName=name)
    if not ctrl.Exists(3, 1):
        ctrl = root.MenuItemControl(SubName=name)
    if not ctrl.Exists(3, 1):
        raise ActionError(f"No clickable control named '{name}'.")
    ctrl.Click()
    return {"clicked": name, "window": title or "desktop"}


def ui_read_window(params: Dict[str, Any]) -> Dict[str, Any]:
    auto = _uia()
    title = str(params.get("title", "")).strip()
    win = auto.WindowControl(searchDepth=1, SubName=title)
    if not win.Exists(3, 1):
        raise ActionError(f"No window matching '{title}'.")
    names = []
    for child in win.GetChildren():
        if child.Name:
            names.append(f"{child.ControlTypeName}: {child.Name}")
    return {"window": win.Name, "controls": names[:60]}


# ---------------------------------------------------------------------------
# Registry
# ---------------------------------------------------------------------------

def build_registry() -> Dict[str, Callable[[Dict[str, Any]], Dict[str, Any]]]:
    import office_actions  # local import to keep COM imports lazy

    return {
        "app.launch": app_launch,
        "app.list_installed": app_list_installed,
        "windows.open": windows_open,
        "system.info": system_info,
        "ui.foreground": ui_foreground,
        "ui.type": ui_type,
        "ui.click": ui_click,
        "ui.read_window": ui_read_window,
        "office.word.create": office_actions.word_create,
        "office.word.save": office_actions.word_save,
        "office.excel.create": office_actions.excel_create,
        "outlook.inbox_summary": office_actions.outlook_inbox_summary,
        "outlook.search": office_actions.outlook_search,
        "outlook.draft": office_actions.outlook_draft,
        "outlook.send": office_actions.outlook_send,
        "memory.get": lambda p: {"value": memory.get(str(p.get("key", "")))},
        "memory.set": lambda p: {"saved": memory.set(str(p.get("key", "")), p.get("value"))},
        "memory.dump": lambda p: memory.dump(),
        "agent.ping": lambda p: {"pong": True, "host": platform.node(), "windows": IS_WINDOWS},
    }
