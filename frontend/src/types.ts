export type NavigationTab = 
  | 'home' 
  | 'computer' 
  | 'developer' 
  | 'media' 
  | 'builder' 
  | 'barehands' 
  | 'system';

export type VoiceState = 
  | 'OFFLINE' 
  | 'CONNECTING' 
  | 'LISTENING' 
  | 'THINKING' 
  | 'SPEAKING' 
  | 'EXECUTING';

/** Shared app category (relocated from the legacy Stage1Intake component). */
export type AppCategory = 'web' | 'mobile' | 'ai';

/** Truthful runtime states of the agent. No fabricated progress. */
export type AgentState =
  | 'WAITING'
  | 'WORKING'
  | 'ACTION_REQUIRED'
  | 'BLOCKED'
  | 'ARTIFACT_READY';

/** Lifecycle of a real artifact. Preview is driven strictly by this. */
export type ArtifactStatus =
  | 'generating'
  | 'unverified'
  | 'verified'
  | 'failed';

export interface Artifact {
  id: string;
  name: string;
  revision: string;
  status: ArtifactStatus;
  createdAt: string;
  /** Real source emitted by an execution run, if any. */
  source?: string;
  /** Machine evidence produced by a real operation. */
  evidence?: string;
  /** Populated only when status is 'failed'. */
  failureReason?: string;
  /** Present only when a real operation exposes measurable progress. */
  progress?: number;
}

export interface ExecutionEvent {
  id: string;
  title: string;
  detail: string;
  timestamp: string;
  status: 'running' | 'completed' | 'blocked' | 'failed';
  evidence?: string;
  progress?: number;
}

/** One flowing console surface: user turns, agent output, execution records. */
export interface TimelineEntry {
  id: string;
  kind: 'user' | 'agent' | 'event';
  text: string;
  timestamp: string;
  detail?: string;
  evidence?: string;
  status?: 'running' | 'completed' | 'blocked' | 'failed';
}

export interface ConversationMessage {
  id: string;
  sender: 'user' | 'jarvis' | 'system';
  text: string;
  timestamp: string;
}

export interface ChatMessage {
  id: string;
  sender: 'jarvis' | 'user' | 'system';
  text: string;
  timestamp: string;
  category?: 'voice' | 'command' | 'dev' | 'system' | 'builder';
}

export interface SystemProcess {
  id: string;
  name: string;
  pid: number;
  cpu: string;
  memory: string;
  status: 'Running' | 'Suspended' | 'Idle';
  category: 'System' | 'App' | 'Developer' | 'Background';
}

export interface ActivityLogItem {
  id: string;
  timestamp: string;
  action: string;
  tool: string;
  status: 'SUCCESS' | 'EXECUTING' | 'GOVERNED' | 'WARNING';
  details: string;
}

export interface BuilderRevision {
  id: string;
  name: string;
  timestamp: string;
  evalScore: number;
  status: 'Candidate' | 'Accepted' | 'Rejected';
  summary: string;
  renderedType: 'dashboard' | 'terminal' | 'hologram' | 'flow';
}

export interface AlexisMediaCandidate {
  id: string;
  scene: number;
  title: string;
  aspectRatio: string;
  status: 'Pending' | 'Approved' | 'Rejected';
  generatedAt: string;
  thumbnailColor: string;
  description: string;
}
