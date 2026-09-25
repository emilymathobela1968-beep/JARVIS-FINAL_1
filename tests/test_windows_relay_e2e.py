"""End-to-end relay test with a SIMULATED Windows agent.

Proves, without a Windows machine: pairing, WebSocket routing, command dispatch,
result return, the confirmation gate, and offline handling. The real agent speaks
exactly this protocol.
"""
import asyncio
import json
import os
import sys

import httpx
import websockets

BASE = os.environ.get("JARVIS_BACKEND_URL") or sys.argv[1]
API = f"{BASE}/api"
WS = API.replace("https://", "wss://").replace("http://", "ws://") + "/windows/agent"

RESULTS = {
    "app.launch": {"launched": "calculator", "route": "known_target", "target": "calc.exe"},
    "windows.open": {"opened": "control_panel", "target": "shell:control"},
    "system.info": {"local_time": "12:00:00", "date": "2026-06-01"},
    "office.word.create": {"created": True, "saved_to": r"C:\Users\Test\Documents\JARVIS\letter.docx"},
    "outlook.send": {"sent": True},
}


async def fake_agent(code: str, ready: asyncio.Event, stop: asyncio.Event):
    async with websockets.connect(WS) as ws:
        await ws.send(json.dumps({
            "type": "hello", "code": code, "hostname": "SIM-WIN11",
            "version": "test", "capabilities": list(RESULTS.keys()),
        }))
        ack = json.loads(await ws.recv())
        assert ack["type"] == "ready", ack
        ready.set()
        while not stop.is_set():
            try:
                raw = await asyncio.wait_for(ws.recv(), timeout=0.5)
            except asyncio.TimeoutError:
                continue
            msg = json.loads(raw)
            if msg.get("type") != "command":
                continue
            data = RESULTS.get(msg["action"])
            await ws.send(json.dumps({
                "type": "result",
                "request_id": msg["request_id"],
                "ok": data is not None,
                "data": data,
                "error": None if data else f"unsupported {msg['action']}",
            }))


async def main() -> int:
    failures = []

    def check(name, cond, detail=""):
        print(f"{'PASS' if cond else 'FAIL'}: {name} {detail}")
        if not cond:
            failures.append(name)

    async with httpx.AsyncClient(timeout=60) as http:
        # offline first
        pair = (await http.post(f"{API}/windows/pair")).json()
        link_id, code = pair["link_id"], pair["code"]
        check("pairing code issued", len(code) == 6 and bool(link_id), f"code={code}")

        st = (await http.get(f"{API}/windows/status", params={"link_id": link_id})).json()
        check("status reports not_running before agent starts", st["state"] == "not_running")

        r = (await http.post(f"{API}/windows/command", json={
            "link_id": link_id, "action": "app.launch", "params": {"app": "calculator"}})).json()
        check("command returns agent_offline when agent is down", r["status"] == "agent_offline")

        ready, stop = asyncio.Event(), asyncio.Event()
        agent_task = asyncio.create_task(fake_agent(code, ready, stop))
        await asyncio.wait_for(ready.wait(), timeout=20)

        st = (await http.get(f"{API}/windows/status", params={"link_id": link_id})).json()
        check("status reports connected after pairing", st["state"] == "connected", st.get("hostname", ""))

        # intent routing through the API
        it = (await http.post(f"{API}/windows/intent", json={"text": "open Control Panel"})).json()
        check("intent maps Control Panel", it["action"] == "windows.open" and it["params"]["target"] == "control_panel")

        # real dispatch round-trips
        for action, params in [
            ("app.launch", {"app": "calculator"}),
            ("windows.open", {"target": "control_panel"}),
            ("system.info", {"what": "time"}),
        ]:
            r = (await http.post(f"{API}/windows/command", json={
                "link_id": link_id, "action": action, "params": params})).json()
            check(f"dispatch {action}", r["status"] == "completed", json.dumps(r.get("data"))[:80])

        # Word document: content is composed server-side then handed to the agent
        r = (await http.post(f"{API}/windows/command", json={
            "link_id": link_id,
            "action": "office.word.create",
            "params": {"instruction": "Write a short resignation letter to Conrad Adams at "
                                      "Paramount Group, effective 30 September. Signed Alex Doe."},
        })).json()
        check("word document dispatched with composed body", r["status"] == "completed",
              str(r.get("data", {}))[:80])

        # confirmation gate
        r = (await http.post(f"{API}/windows/command", json={
            "link_id": link_id, "action": "outlook.send",
            "params": {"to": "conrad@example.com", "body": "text", "subject": "Notice"}})).json()
        check("outlook.send blocked without confirmation", r["status"] == "confirmation_required")

        r = (await http.post(f"{API}/windows/command", json={
            "link_id": link_id, "action": "outlook.send", "confirmed": True,
            "params": {"to": "conrad@example.com", "body": "text", "subject": "Notice"}})).json()
        check("outlook.send executes once confirmed", r["status"] == "completed")

        log = (await http.get(f"{API}/windows/log", params={"link_id": link_id})).json()
        check("command log recorded", len(log["commands"]) >= 5, f"{len(log['commands'])} rows")

        stop.set()
        await asyncio.sleep(0.6)
        agent_task.cancel()

        st = (await http.get(f"{API}/windows/status", params={"link_id": link_id})).json()
        check("status returns to not_running after agent exits", st["state"] == "not_running")

    print("\n" + ("ALL RELAY TESTS PASSED" if not failures else f"FAILURES: {failures}"))
    return 0 if not failures else 1


if __name__ == "__main__":
    sys.exit(asyncio.run(main()))
