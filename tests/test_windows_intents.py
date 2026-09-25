"""Unit tests for the Windows intent router (runs on any OS)."""
import sys
sys.path.insert(0, "/app/backend")

from windows_intents import parse_intent  # noqa: E402

CASES = [
    ("Open Word", "app.launch", {"app": "winword"}),
    ("open excel", "app.launch", {"app": "excel"}),
    ("Open Calculator", "app.launch", {"app": "calculator"}),
    ("open notepad", "app.launch", {"app": "notepad"}),
    ("Open Control Panel", "windows.open", {"target": "control_panel"}),
    ("Open Device Manager", "windows.open", {"target": "device_manager"}),
    ("go to network settings", "windows.open", {"target": "network"}),
    ("open bluetooth settings", "windows.open", {"target": "bluetooth"}),
    ("open task manager", "windows.open", {"target": "task_manager"}),
    ("check the time", "system.info", {"what": "time"}),
    ("check disk space", "system.info", {"what": "disk"}),
    ("show running processes", "system.info", {"what": "processes"}),
    ("what is my battery level", "system.info", {"what": "battery"}),
    ("check my emails", "outlook.inbox_summary", None),
    ("open Spotify", "app.launch", {"app": "spotify"}),
]


def test_known_routes():
    for text, action, params in CASES:
        intent = parse_intent(text)
        assert intent is not None, f"no intent for {text!r}"
        assert intent.action == action, f"{text!r} -> {intent.action} (expected {action})"
        if params:
            for k, v in params.items():
                assert intent.params.get(k) == v, f"{text!r} param {k}={intent.params.get(k)!r}"


def test_word_letter_intent():
    intent = parse_intent(
        "Write a resignation letter to Conrad Adams at Paramount Group, effective 30 September"
    )
    assert intent is not None
    assert intent.action == "office.word.create"
    assert "Conrad Adams" in intent.params["instruction"]
    assert intent.requires_confirmation is False


def test_email_draft_never_sends_without_confirmation():
    intent = parse_intent("send an email to conrad@paramount.com about my notice period")
    assert intent is not None
    assert intent.action == "outlook.draft"  # draft, never a direct send
    assert intent.params["to"] == "conrad@paramount.com"


def test_send_action_requires_confirmation():
    from windows_intents import CONFIRM_REQUIRED

    assert "outlook.send" in CONFIRM_REQUIRED
    assert "shell.run" in CONFIRM_REQUIRED


def test_unknown_returns_none():
    assert parse_intent("tell me a joke about quantum physics") is None
    assert parse_intent("") is None


if __name__ == "__main__":
    test_known_routes()
    test_word_letter_intent()
    test_email_draft_never_sends_without_confirmation()
    test_send_action_requires_confirmation()
    test_unknown_returns_none()
    print("PASS: all intent routing tests")
