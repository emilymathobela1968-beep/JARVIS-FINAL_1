// Backend API helpers. Backend URL comes ONLY from env (never hardcoded).
const BACKEND_URL = (import.meta as any).env?.REACT_APP_BACKEND_URL as string;
export const API_BASE = `${BACKEND_URL}/api`;

export interface GenerateBody {
  objective: string;
  app_type?: string;
  session_id?: string;
}

export interface GenerateCallbacks {
  onStart?: (d: { id: string; session_id: string }) => void;
  onDelta?: (content: string) => void;
  onDone?: (d: { id: string; source: string; evidence?: string }) => void;
  onError?: (d: { message: string; evidence?: string }) => void;
}

function dispatch(rawEvent: string, cb: GenerateCallbacks) {
  const lines = rawEvent.split('\n');
  let event = 'message';
  let data = '';
  for (const line of lines) {
    if (line.startsWith('event:')) event = line.slice(6).trim();
    else if (line.startsWith('data:')) data += line.slice(5).trim();
  }
  if (!data) return;
  let parsed: any;
  try {
    parsed = JSON.parse(data);
  } catch {
    return;
  }
  if (event === 'start') cb.onStart?.(parsed);
  else if (event === 'delta') cb.onDelta?.(parsed.content ?? '');
  else if (event === 'done') cb.onDone?.(parsed);
  else if (event === 'error') cb.onError?.(parsed);
}

/**
 * Streams a web-app generation. Returns an AbortController so the caller can stop it.
 */
export function generateWebApp(body: GenerateBody, cb: GenerateCallbacks): AbortController {
  const controller = new AbortController();

  (async () => {
    try {
      const res = await fetch(`${API_BASE}/builder/generate`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ app_type: 'web', ...body }),
        signal: controller.signal,
      });

      if (!res.ok || !res.body) {
        let detail = `Request failed (HTTP ${res.status})`;
        try {
          const j = await res.json();
          if (j?.detail) detail = j.detail;
        } catch {
          /* ignore */
        }
        cb.onError?.({ message: detail, evidence: `status=${res.status}` });
        return;
      }

      const reader = res.body.getReader();
      const decoder = new TextDecoder();
      let buffer = '';

      while (true) {
        const { done, value } = await reader.read();
        if (done) break;
        buffer += decoder.decode(value, { stream: true });
        const chunks = buffer.split('\n\n');
        buffer = chunks.pop() || '';
        for (const chunk of chunks) {
          if (chunk.trim()) dispatch(chunk, cb);
        }
      }
      if (buffer.trim()) dispatch(buffer, cb);
    } catch (e: any) {
      if (e?.name !== 'AbortError') {
        cb.onError?.({ message: e?.message || 'Network error reaching JARVIS backend.' });
      }
    }
  })();

  return controller;
}

export async function markVerified(id: string): Promise<void> {
  try {
    await fetch(`${API_BASE}/builder/verify/${id}`, { method: 'POST' });
  } catch {
    /* verification record is best-effort */
  }
}

// ---------------------------------------------------------------------------
// Native image generation (provider key stays server-side)
// ---------------------------------------------------------------------------

export interface ImageRequest {
  prompt: string;
  style: string;
  aspect_ratio: string;
  quality: string;
}

export interface ImageResult {
  id: string;
  status: 'generating' | 'completed' | 'failed' | 'blocked';
  image_url: string | null;
  error: string | null;
}

/** Absolute URL for a file served by the backend. */
export const absoluteUrl = (path: string) => `${BACKEND_URL}${path}`;

export async function generateImage(body: ImageRequest): Promise<ImageResult> {
  const res = await fetch(`${API_BASE}/image-generation/generate`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  });
  if (!res.ok) {
    let detail = `Request failed (HTTP ${res.status})`;
    try {
      const j = await res.json();
      if (j?.detail) detail = typeof j.detail === 'string' ? j.detail : JSON.stringify(j.detail);
    } catch {
      /* ignore */
    }
    return { id: '', status: 'failed', image_url: null, error: detail };
  }
  return res.json();
}

export async function imageProviderStatus(): Promise<{ provider: string; model: string; connected: boolean }> {
  const res = await fetch(`${API_BASE}/image-generation/status`);
  return res.json();
}

// ---------------------------------------------------------------------------
// Windows companion agent (relay)
// ---------------------------------------------------------------------------

export interface AgentStatus {
  state: 'connected' | 'not_running';
  link_id?: string | null;
  hostname?: string;
  version?: string;
  capabilities?: string[];
  agents_online?: number;
}

export interface WindowsIntent {
  understood: boolean;
  action?: string;
  params?: Record<string, unknown>;
  summary?: string;
  requires_confirmation?: boolean;
  message?: string;
}

export interface WindowsCommandResult {
  status: 'completed' | 'failed' | 'agent_offline' | 'confirmation_required';
  id?: string;
  action?: string;
  data?: unknown;
  error?: string | null;
  message?: string;
}

export async function windowsPair(): Promise<{ link_id: string; code: string; expires_at: string }> {
  const res = await fetch(`${API_BASE}/windows/pair`, { method: 'POST' });
  return res.json();
}

export async function windowsStatus(linkId?: string | null): Promise<AgentStatus> {
  const q = linkId ? `?link_id=${encodeURIComponent(linkId)}` : '';
  const res = await fetch(`${API_BASE}/windows/status${q}`);
  return res.json();
}

export async function windowsIntent(text: string): Promise<WindowsIntent> {
  const res = await fetch(`${API_BASE}/windows/intent`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ text }),
  });
  return res.json();
}

export async function windowsCommand(
  linkId: string,
  action: string,
  params: Record<string, unknown> = {},
  confirmed = false
): Promise<WindowsCommandResult> {
  const res = await fetch(`${API_BASE}/windows/command`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ link_id: linkId, action, params, confirmed }),
  });
  return res.json();
}

/**
 * Injects a tiny probe that reports real render + first interaction from inside the
 * sandboxed iframe via postMessage. The probe is added ONLY to the render copy — the
 * inspectable source stays exactly as the model produced it.
 */
export function withVerificationProbe(source: string): string {
  const probe = `
<script>(function(){
  function post(type){try{window.parent.postMessage({__jarvis:true,type:type},'*');}catch(e){}}
  if(document.readyState==='complete'){post('render');}else{window.addEventListener('load',function(){post('render');});}
  var fired=false;
  function onInteract(){if(fired)return;fired=true;post('interaction');}
  ['pointerdown','click','keydown','input','change'].forEach(function(ev){
    document.addEventListener(ev,onInteract,true);
  });
})();<\/script>`;
  if (/<\/body>/i.test(source)) {
    return source.replace(/<\/body>/i, `${probe}\n</body>`);
  }
  return source + probe;
}
