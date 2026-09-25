"""JARVIS Windows companion agent.

Dials OUT to the JARVIS backend over a secure WebSocket using a one-time pairing
code, then executes Windows actions locally and reports results back.

Also exposes a read-only local status API on 127.0.0.1 (loopback only) so you can
check the agent from the same machine.

Run:  python jarvis_agent.py --code 123456
Env:  JARVIS_BACKEND_URL, JARVIS_PAIR_CODE
"""
from __future__ import annotations

import argparse
import asyncio
import json
import logging
import os
import platform
import sys
import threading
from datetime import datetime, timezone
from http.server import BaseHTTPRequestHandler, HTTPServer

import websockets

from actions import ActionError, build_registry, memory

VERSION = "1.0.0"
LOG = logging.getLogger("jarvis-agent")

STATE = {"connected": False, "link_id": None, "last_command": None, "started_at": None}


def ws_url(backend: str) -> str:
    base = backend.rstrip("/")
    if base.startswith("https://"):
        return "wss://" + base[len("https://"):] + "/api/windows/agent"
    if base.startswith("http://"):
        return "ws://" + base[len("http://"):] + "/api/windows/agent"
    return base + "/api/windows/agent"


class StatusHandler(BaseHTTPRequestHandler):
    def do_GET(self):  # noqa: N802
        payload = json.dumps({**STATE, "version": VERSION, "host": platform.node()}).encode()
        self.send_response(200)
        self.send_header("Content-Type", "application/json")
        self.send_header("Content-Length", str(len(payload)))
        self.end_headers()
        self.wfile.write(payload)

    def log_message(self, *_args):  # silence
        return


def start_local_status_server(port: int = 8765):
    server = HTTPServer(("127.0.0.1", port), StatusHandler)
    threading.Thread(target=server.serve_forever, daemon=True).start()
    LOG.info("Local status endpoint: http://127.0.0.1:%s/", port)


async def handle_command(registry, msg: dict) -> dict:
    action = msg.get("action", "")
    params = msg.get("params") or {}
    request_id = msg.get("request_id")

    handler = registry.get(action)
    if not handler:
        return {"type": "result", "request_id": request_id, "ok": False,
                "error": f"Unknown action '{action}'."}

    STATE["last_command"] = {"action": action, "at": datetime.now(timezone.utc).isoformat()}
    LOG.info("Executing %s %s", action, json.dumps(params)[:200])
    try:
        data = await asyncio.to_thread(handler, params)
        memory.remember_task(action, True, json.dumps(data)[:180])
        return {"type": "result", "request_id": request_id, "ok": True, "data": data}
    except ActionError as e:
        memory.remember_task(action, False, str(e))
        return {"type": "result", "request_id": request_id, "ok": False, "error": str(e)}
    except Exception as e:  # noqa: BLE001
        LOG.exception("Action failed")
        memory.remember_task(action, False, str(e))
        return {"type": "result", "request_id": request_id, "ok": False,
                "error": f"{type(e).__name__}: {e}"}


async def run(backend: str, code: str):
    registry = build_registry()
    url = ws_url(backend)
    STATE["started_at"] = datetime.now(timezone.utc).isoformat()
    LOG.info("Connecting to %s", url)

    async with websockets.connect(url, ping_interval=20, max_size=8_000_000) as ws:
        await ws.send(json.dumps({
            "type": "hello",
            "code": code,
            "hostname": platform.node(),
            "version": VERSION,
            "capabilities": sorted(registry.keys()),
        }))
        ack = json.loads(await ws.recv())
        if ack.get("type") != "ready":
            LOG.error("Pairing rejected: %s", ack.get("message"))
            return 2

        STATE["connected"] = True
        STATE["link_id"] = ack.get("link_id")
        LOG.info("Paired. JARVIS can now control this machine. link_id=%s", ack.get("link_id"))

        async for raw in ws:
            msg = json.loads(raw)
            if msg.get("type") == "command":
                result = await handle_command(registry, msg)
                await ws.send(json.dumps(result))
            elif msg.get("type") == "pong":
                continue
    STATE["connected"] = False
    return 0


def main() -> int:
    logging.basicConfig(level=logging.INFO, format="%(asctime)s  %(levelname)s  %(message)s")
    parser = argparse.ArgumentParser(description="JARVIS Windows companion agent")
    parser.add_argument("--code", default=os.environ.get("JARVIS_PAIR_CODE", ""),
                        help="One-time pairing code shown in JARVIS ▸ Computer")
    parser.add_argument("--backend", default=os.environ.get("JARVIS_BACKEND_URL", ""),
                        help="JARVIS backend base URL (https://...)")
    parser.add_argument("--status-port", type=int, default=int(os.environ.get("JARVIS_AGENT_PORT", "8765")))
    args = parser.parse_args()

    if not args.backend:
        print("ERROR: set JARVIS_BACKEND_URL or pass --backend https://your-jarvis-url")
        return 1
    code = args.code.strip()
    if not code:
        code = input("Pairing code from JARVIS ▸ Computer: ").strip()
    if not code:
        print("ERROR: a pairing code is required.")
        return 1

    start_local_status_server(args.status_port)
    try:
        return asyncio.run(run(args.backend, code))
    except KeyboardInterrupt:
        return 0
    except Exception as e:  # noqa: BLE001
        LOG.error("Agent stopped: %s", e)
        return 3


if __name__ == "__main__":
    sys.exit(main())
