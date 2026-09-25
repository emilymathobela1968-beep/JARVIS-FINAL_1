#====================================================================================================
# START - Testing Protocol - DO NOT EDIT OR REMOVE THIS SECTION
#====================================================================================================

# THIS SECTION CONTAINS CRITICAL TESTING INSTRUCTIONS FOR BOTH AGENTS
# BOTH MAIN_AGENT AND TESTING_AGENT MUST PRESERVE THIS ENTIRE BLOCK

# Communication Protocol:
# If the `testing_agent` is available, main agent should delegate all testing tasks to it.
#
# You have access to a file called `test_result.md`. This file contains the complete testing state
# and history, and is the primary means of communication between main and the testing agent.
#
# Main and testing agents must follow this exact format to maintain testing data. 
# The testing data must be entered in yaml format Below is the data structure:
# 
## user_problem_statement: {problem_statement}
## backend:
##   - task: "Task name"
##     implemented: true
##     working: true  # or false or "NA"
##     file: "file_path.py"
##     stuck_count: 0
##     priority: "high"  # or "medium" or "low"
##     needs_retesting: false
##     status_history:
##         -working: true  # or false or "NA"
##         -agent: "main"  # or "testing" or "user"
##         -comment: "Detailed comment about status"
##
## frontend:
##   - task: "Task name"
##     implemented: true
##     working: true  # or false or "NA"
##     file: "file_path.js"
##     stuck_count: 0
##     priority: "high"  # or "medium" or "low"
##     needs_retesting: false
##     status_history:
##         -working: true  # or false or "NA"
##         -agent: "main"  # or "testing" or "user"
##         -comment: "Detailed comment about status"
##
## metadata:
##   created_by: "main_agent"
##   version: "1.0"
##   test_sequence: 0
##   run_ui: false
##
## test_plan:
##   current_focus:
##     - "Task name 1"
##     - "Task name 2"
##   stuck_tasks:
##     - "Task name with persistent issues"
##   test_all: false
##   test_priority: "high_first"  # or "sequential" or "stuck_first"
##
## agent_communication:
##     -agent: "main"  # or "testing" or "user"
##     -message: "Communication message between agents"

# Protocol Guidelines for Main agent
#
# 1. Update Test Result File Before Testing:
#    - Main agent must always update the `test_result.md` file before calling the testing agent
#    - Add implementation details to the status_history
#    - Set `needs_retesting` to true for tasks that need testing
#    - Update the `test_plan` section to guide testing priorities
#    - Add a message to `agent_communication` explaining what you've done
#
# 2. Incorporate User Feedback:
#    - When a user provides feedback that something is or isn't working, add this information to the relevant task's status_history
#    - Update the working status based on user feedback
#    - If a user reports an issue with a task that was marked as working, increment the stuck_count
#    - Whenever user reports issue in the app, if we have testing agent and task_result.md file so find the appropriate task for that and append in status_history of that task to contain the user concern and problem as well 
#
# 3. Track Stuck Tasks:
#    - Monitor which tasks have high stuck_count values or where you are fixing same issue again and again, analyze that when you read task_result.md
#    - For persistent issues, use websearch tool to find solutions
#    - Pay special attention to tasks in the stuck_tasks list
#    - When you fix an issue with a stuck task, don't reset the stuck_count until the testing agent confirms it's working
#
# 4. Provide Context to Testing Agent:
#    - When calling the testing agent, provide clear instructions about:
#      - Which tasks need testing (reference the test_plan)
#      - Any authentication details or configuration needed
#      - Specific test scenarios to focus on
#      - Any known issues or edge cases to verify
#
# 5. Call the testing agent with specific instructions referring to test_result.md
#
# IMPORTANT: Main agent must ALWAYS update test_result.md BEFORE calling the testing agent, as it relies on this file to understand what to test next.

#====================================================================================================
# END - Testing Protocol - DO NOT EDIT OR REMOVE THIS SECTION
#====================================================================================================



#====================================================================================================
# Testing Data - Main Agent and testing sub agent both should log testing data below this section
#====================================================================================================

user_problem_statement: "Milestone 1 — JARVIS Web App builder: Home -> Builder -> Workstation, real LLM-generated single-page Web App preview rendered in a sandboxed iframe, verified only after real render + interaction, Full Screen/Exit, edit objective, honest blocked/failed states. Uses Emergent universal key + gpt-5.4. Mobile App / AI Model / Media / saved history / deployment are deferred and must be visibly marked unavailable."

backend:
  - task: "POST /api/builder/generate — SSE web app generation via gpt-5.4 (emergentintegrations)"
    implemented: true
    working: true
    file: "backend/server.py"
    stuck_count: 0
    priority: "high"
    needs_retesting: false
    status_history:
        -working: "NA"
        -agent: "main"
        -comment: "New SSE endpoint. Streams event: start/delta/done/error. Generates a single self-contained HTML doc. Persists to db.generations. Manually smoke-tested via curl: streams deltas and returns full HTML in done event."
        -working: true
        -agent: "testing"
        -comment: "✅ PASSED comprehensive SSE testing. Endpoint correctly streams: event:start with id & session_id, 1682 event:delta frames, event:done with valid 5832-char HTML starting with '<!DOCTYPE html>'. No error events. Real LLM (gpt-5.4) generated a complete counter web app. Generation ID captured: 8823734c-cac1-4c49-8846-8b15c4657af3"
  - task: "Unsupported app_type honest rejection (mobile/ai -> HTTP 422)"
    implemented: true
    working: true
    file: "backend/server.py"
    stuck_count: 0
    priority: "high"
    needs_retesting: false
    status_history:
        -working: "NA"
        -agent: "main"
        -comment: "app_type not in {web} returns 422 with detail. Empty objective returns 400. Manually verified mobile returns 422."
        -working: true
        -agent: "testing"
        -comment: "✅ PASSED validation tests. app_type='mobile' returns HTTP 422 with detail: 'mobile generation is not available in this milestone. Only Web App is supported.' Empty objective returns HTTP 400 as expected."
  - task: "POST /api/builder/verify/{id} — records verified state"
    implemented: true
    working: true
    file: "backend/server.py"
    stuck_count: 0
    priority: "medium"
    needs_retesting: false
    status_history:
        -working: "NA"
        -agent: "main"
        -comment: "Marks generation verified; 404 for unknown id."
        -working: true
        -agent: "testing"
        -comment: "✅ PASSED verify endpoint tests. Valid generation_id (8823734c-cac1-4c49-8846-8b15c4657af3) returns HTTP 200 with {id, status:'verified'}. Invalid ID 'does-not-exist-123' correctly returns HTTP 404."

frontend:
  - task: "Builder generation flow + sandbox verify (render+interaction) + honest states"
    implemented: true
    working: true
    file: "frontend/src/components/Stage2Workspace.tsx, frontend/src/components/GeneratedArtifact.tsx, frontend/src/utils/api.ts"
    stuck_count: 0
    priority: "high"
    needs_retesting: false
    status_history:
        -working: true
        -agent: "main"
        -comment: "Verified via screenshot automation: to-do + tip-calculator apps generated live, rendered in sandbox (unverified), real iframe click flipped to verified. Mobile App type shows honest BLOCKED state. Full Screen/Inspect/Edit objective controls present."

metadata:
  created_by: "main_agent"
  version: "1.0"
  test_sequence: 1
  run_ui: false

test_plan:
  current_focus:
    - "POST /api/builder/generate — SSE web app generation via gpt-5.4 (emergentintegrations)"
    - "Unsupported app_type honest rejection (mobile/ai -> HTTP 422)"
    - "POST /api/builder/verify/{id} — records verified state"
  stuck_tasks: []
  test_all: false
  test_priority: "high_first"

agent_communication:
    -agent: "main"
    -message: "Please test the 3 new backend endpoints only. 1) POST /api/builder/generate with body {objective:'a simple counter app', app_type:'web'} — it is an SSE (text/event-stream) response; verify it emits event: start, one or more event: delta, and a final event: done whose data.source contains a valid HTML document starting with <!DOCTYPE html>. Note this calls a real LLM (gpt-5.4) so allow up to ~90s. 2) POST /api/builder/generate with app_type:'mobile' must return HTTP 422; empty objective must return 400. 3) POST /api/builder/verify/{id} using an id from a prior generate should return status verified; a random/unknown id returns 404. Do not test frontend."
    -agent: "testing"
    -message: "✅ ALL 3 BACKEND ENDPOINTS PASSED. Test 1: SSE generation endpoint working perfectly - streamed 1682 delta events and returned valid 5832-char HTML counter app via real gpt-5.4 LLM. Test 2: Validation working - mobile returns 422 with honest message, empty objective returns 400. Test 3: Verify endpoint working - valid ID returns 200 with verified status, invalid ID returns 404. No issues found. Backend implementation is solid and ready for production."
