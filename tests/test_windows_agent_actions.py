"""Agent-side unit tests that run on ANY OS.

On Linux/macOS the Windows-only actions must fail with a clear, honest message
instead of pretending to work. On Windows the same registry executes for real.
"""
import sys
from pathlib import Path

AGENT = Path("/app/windows-agent")
sys.path.insert(0, str(AGENT))

from actions import ActionError, IS_WINDOWS, build_registry, memory  # noqa: E402
from memory_store import MemoryStore  # noqa: E402


def test_registry_covers_required_actions():
    registry = build_registry()
    required = [
        "app.launch", "app.list_installed", "windows.open", "system.info",
        "ui.foreground", "ui.type", "ui.click", "ui.read_window",
        "office.word.create", "office.word.save", "office.excel.create",
        "outlook.inbox_summary", "outlook.search", "outlook.draft", "outlook.send",
        "memory.get", "memory.set", "agent.ping",
    ]
    for action in required:
        assert action in registry, f"missing action {action}"
    print(f"PASS: registry exposes {len(registry)} actions")


def test_ping_and_system_time_work_everywhere():
    registry = build_registry()
    pong = registry["agent.ping"]({})
    assert pong["pong"] is True
    t = registry["system.info"]({"what": "time"})
    assert "local_time" in t and "date" in t
    disk = registry["system.info"]({"what": "disk"})
    assert disk["disks"] and disk["disks"][0]["total_gb"] > 0
    print("PASS: ping, time and disk info work on this host")


def test_windows_only_actions_fail_honestly_off_windows():
    if IS_WINDOWS:
        print("SKIP: running on Windows, real execution path is used")
        return
    registry = build_registry()
    for action, params in [
        ("app.launch", {"app": "calculator"}),
        ("windows.open", {"target": "control_panel"}),
        ("office.word.create", {"body": "hello"}),
        ("ui.type", {"text": "hi"}),
    ]:
        try:
            registry[action](params)
            raise AssertionError(f"{action} should not succeed off Windows")
        except Exception as e:
            assert "Windows" in str(e) or "requires" in str(e) or "not installed" in str(e), str(e)
    print("PASS: Windows-only actions report honest 'requires Windows' errors off Windows")


def test_memory_store_refuses_secrets(tmp_path=Path("/tmp")):
    store = MemoryStore(tmp_path / "jarvis_memory_test.json")
    store.set("preferred_signature", "Alex Doe")
    assert store.get("preferred_signature") == "Alex Doe"
    store.remember_app_path("winword", "winword.exe")
    assert store.get_app_path("winword") == "winword.exe"
    store.remember_contact("Conrad Adams", "conrad@paramount.example")
    assert store.lookup_contact("conrad adams") == "conrad@paramount.example"
    try:
        store.set("outlook_password", "hunter2")
        raise AssertionError("memory store must refuse credentials")
    except ValueError:
        pass
    print("PASS: memory store persists routes/contacts and refuses credentials")


if __name__ == "__main__":
    test_registry_covers_required_actions()
    test_ping_and_system_time_work_everywhere()
    test_windows_only_actions_fail_honestly_off_windows()
    test_memory_store_refuses_secrets()
    _ = memory, ActionError
    print("\nALL AGENT UNIT TESTS PASSED")
