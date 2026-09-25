#!/usr/bin/env python3
"""
Backend API tests for JARVIS Builder endpoints.
Tests the 3 new builder endpoints: generate (SSE), validation, and verify.
"""

import requests
import json
import time
import os
from pathlib import Path
from dotenv import load_dotenv

# Load frontend .env to get REACT_APP_BACKEND_URL
frontend_env = Path("/app/frontend/.env")
load_dotenv(frontend_env)

BACKEND_URL = os.environ.get("REACT_APP_BACKEND_URL", "").rstrip("/") + "/api"

print(f"Testing backend at: {BACKEND_URL}")
print("=" * 80)


def test_builder_generate_valid():
    """
    Test 1: POST /api/builder/generate with valid web app objective.
    This is an SSE endpoint that streams events.
    Expected: event: start, event: delta (one or more), event: done with HTML source.
    """
    print("\n[TEST 1] POST /api/builder/generate - Valid web app generation")
    print("-" * 80)
    
    url = f"{BACKEND_URL}/builder/generate"
    payload = {
        "objective": "a simple counter web app with increment and reset buttons",
        "app_type": "web"
    }
    
    print(f"URL: {url}")
    print(f"Payload: {json.dumps(payload, indent=2)}")
    print("Note: This calls a real LLM (gpt-5.4), allowing up to 90 seconds...")
    
    try:
        response = requests.post(
            url,
            json=payload,
            headers={"Accept": "text/event-stream"},
            stream=True,
            timeout=120  # Allow up to 120 seconds for LLM response
        )
        
        print(f"Status Code: {response.status_code}")
        print(f"Content-Type: {response.headers.get('content-type', 'N/A')}")
        
        if response.status_code != 200:
            print(f"❌ FAILED: Expected 200, got {response.status_code}")
            print(f"Response: {response.text}")
            return None
        
        # Parse SSE stream
        events = []
        generation_id = None
        session_id = None
        has_start = False
        has_delta = False
        has_done = False
        has_error = False
        done_source = None
        
        print("\nStreaming events:")
        for line in response.iter_lines(decode_unicode=True):
            if not line:
                continue
            
            if line.startswith("event: "):
                event_type = line[7:].strip()
                events.append({"type": event_type})
                print(f"  Event: {event_type}")
                
                if event_type == "start":
                    has_start = True
                elif event_type == "delta":
                    has_delta = True
                elif event_type == "done":
                    has_done = True
                elif event_type == "error":
                    has_error = True
                    
            elif line.startswith("data: "):
                data_str = line[6:].strip()
                try:
                    data = json.loads(data_str)
                    if events:
                        events[-1]["data"] = data
                        
                        # Capture generation_id and session_id from start event
                        if events[-1]["type"] == "start":
                            generation_id = data.get("id")
                            session_id = data.get("session_id")
                            print(f"    → id: {generation_id}")
                            print(f"    → session_id: {session_id}")
                        
                        # Capture source from done event
                        elif events[-1]["type"] == "done":
                            done_source = data.get("source", "")
                            print(f"    → source length: {len(done_source)} chars")
                            if done_source:
                                preview = done_source[:100].replace("\n", " ")
                                print(f"    → source preview: {preview}...")
                        
                        # Show delta content (truncated)
                        elif events[-1]["type"] == "delta":
                            content = data.get("content", "")
                            if content:
                                print(f"    → content: {content[:50]}..." if len(content) > 50 else f"    → content: {content}")
                        
                        # Show error details
                        elif events[-1]["type"] == "error":
                            print(f"    → ERROR: {data}")
                            
                except json.JSONDecodeError:
                    print(f"    → (non-JSON data)")
        
        print(f"\nTotal events received: {len(events)}")
        print(f"Has 'start' event: {has_start}")
        print(f"Has 'delta' event: {has_delta}")
        print(f"Has 'done' event: {has_done}")
        print(f"Has 'error' event: {has_error}")
        
        # Validation
        if not has_start:
            print("❌ FAILED: Missing 'event: start' frame")
            return None
        
        if not generation_id or not session_id:
            print("❌ FAILED: 'start' event missing id or session_id")
            return None
        
        if not has_delta:
            print("❌ FAILED: No 'event: delta' frames received")
            return None
        
        if has_error:
            print("❌ FAILED: Received 'event: error' frame")
            return None
        
        if not has_done:
            print("❌ FAILED: Missing 'event: done' frame")
            return None
        
        if not done_source:
            print("❌ FAILED: 'done' event has no source")
            return None
        
        # Validate HTML structure
        done_source_lower = done_source.lower().strip()
        if not (done_source_lower.startswith("<!doctype html") or done_source_lower.startswith("<html")):
            print(f"❌ FAILED: 'done' source does not start with valid HTML")
            print(f"   Source starts with: {done_source[:100]}")
            return None
        
        print("✅ PASSED: Valid SSE stream with HTML source")
        return generation_id
        
    except requests.exceptions.Timeout:
        print("❌ FAILED: Request timed out (>120s)")
        return None
    except Exception as e:
        print(f"❌ FAILED: Exception occurred: {e}")
        import traceback
        traceback.print_exc()
        return None


def test_builder_generate_validation():
    """
    Test 2: Validation tests for /api/builder/generate
    - app_type='mobile' should return HTTP 422
    - empty objective should return HTTP 400
    """
    print("\n[TEST 2] POST /api/builder/generate - Validation tests")
    print("-" * 80)
    
    url = f"{BACKEND_URL}/builder/generate"
    
    # Test 2a: Unsupported app_type (mobile)
    print("\n[TEST 2a] Unsupported app_type='mobile'")
    payload = {
        "objective": "x",
        "app_type": "mobile"
    }
    print(f"Payload: {json.dumps(payload, indent=2)}")
    
    try:
        response = requests.post(url, json=payload, timeout=10)
        print(f"Status Code: {response.status_code}")
        
        if response.status_code != 422:
            print(f"❌ FAILED: Expected 422, got {response.status_code}")
            print(f"Response: {response.text}")
            return False
        
        try:
            data = response.json()
            detail = data.get("detail", "")
            print(f"Response detail: {detail}")
            
            if "not available" not in detail.lower() or "web app" not in detail.lower():
                print(f"❌ FAILED: Expected detail to mention 'not available' and 'Web App'")
                return False
            
            print("✅ PASSED: Returns 422 with appropriate message for unsupported app_type")
        except json.JSONDecodeError:
            print(f"❌ FAILED: Response is not valid JSON")
            return False
            
    except Exception as e:
        print(f"❌ FAILED: Exception occurred: {e}")
        return False
    
    # Test 2b: Empty objective
    print("\n[TEST 2b] Empty objective")
    payload = {
        "objective": "",
        "app_type": "web"
    }
    print(f"Payload: {json.dumps(payload, indent=2)}")
    
    try:
        response = requests.post(url, json=payload, timeout=10)
        print(f"Status Code: {response.status_code}")
        
        if response.status_code != 400:
            print(f"❌ FAILED: Expected 400, got {response.status_code}")
            print(f"Response: {response.text}")
            return False
        
        print("✅ PASSED: Returns 400 for empty objective")
        return True
        
    except Exception as e:
        print(f"❌ FAILED: Exception occurred: {e}")
        return False


def test_builder_verify(generation_id):
    """
    Test 3: POST /api/builder/verify/{id}
    - Valid ID should return 200 with status='verified'
    - Invalid ID should return 404
    """
    print("\n[TEST 3] POST /api/builder/verify/{id}")
    print("-" * 80)
    
    # Test 3a: Valid ID
    if generation_id:
        print(f"\n[TEST 3a] Valid generation_id: {generation_id}")
        url = f"{BACKEND_URL}/builder/verify/{generation_id}"
        print(f"URL: {url}")
        
        try:
            response = requests.post(url, timeout=10)
            print(f"Status Code: {response.status_code}")
            
            if response.status_code != 200:
                print(f"❌ FAILED: Expected 200, got {response.status_code}")
                print(f"Response: {response.text}")
            else:
                try:
                    data = response.json()
                    print(f"Response: {json.dumps(data, indent=2)}")
                    
                    if data.get("id") != generation_id:
                        print(f"❌ FAILED: Response id doesn't match")
                        return False
                    
                    if data.get("status") != "verified":
                        print(f"❌ FAILED: Expected status='verified', got '{data.get('status')}'")
                        return False
                    
                    print("✅ PASSED: Valid ID returns 200 with status='verified'")
                except json.JSONDecodeError:
                    print(f"❌ FAILED: Response is not valid JSON")
                    return False
                    
        except Exception as e:
            print(f"❌ FAILED: Exception occurred: {e}")
            return False
    else:
        print("\n[TEST 3a] SKIPPED: No valid generation_id from Test 1")
    
    # Test 3b: Invalid ID
    print("\n[TEST 3b] Invalid generation_id")
    invalid_id = "does-not-exist-123"
    url = f"{BACKEND_URL}/builder/verify/{invalid_id}"
    print(f"URL: {url}")
    
    try:
        response = requests.post(url, timeout=10)
        print(f"Status Code: {response.status_code}")
        
        if response.status_code != 404:
            print(f"❌ FAILED: Expected 404, got {response.status_code}")
            print(f"Response: {response.text}")
            return False
        
        print("✅ PASSED: Invalid ID returns 404")
        return True
        
    except Exception as e:
        print(f"❌ FAILED: Exception occurred: {e}")
        return False


def main():
    print("\n" + "=" * 80)
    print("JARVIS BUILDER BACKEND API TESTS")
    print("=" * 80)
    
    # Test 1: Valid generation (SSE)
    generation_id = test_builder_generate_valid()
    
    # Test 2: Validation
    test_builder_generate_validation()
    
    # Test 3: Verify endpoint
    test_builder_verify(generation_id)
    
    print("\n" + "=" * 80)
    print("TESTS COMPLETE")
    print("=" * 80)


if __name__ == "__main__":
    main()
