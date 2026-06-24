/** Default command timeouts (ms). Override via env vars or per-call timeoutMs. */

const DEFAULT_MS = parseInt(process.env.CIVIL3D_DEFAULT_TIMEOUT_MS ?? "120000", 10);
const EXECUTE_MS = parseInt(process.env.CIVIL3D_EXECUTE_TIMEOUT_MS ?? String(DEFAULT_MS), 10);
const COMMAND_MS = parseInt(process.env.CIVIL3D_COMMAND_TIMEOUT_MS ?? "300000", 10);
const DISCOVER_MS = parseInt(process.env.CIVIL3D_DISCOVER_TIMEOUT_MS ?? "60000", 10);

export function getCommandTimeoutMs(method: string, overrideMs?: number): number {
  if (overrideMs != null && overrideMs > 0) return overrideMs;

  switch (method) {
    case "executeCode":
    case "sessionExecute":
      return EXECUTE_MS;
    case "executeNativeCommand":
      return COMMAND_MS;
    case "discoverDrawing":
      return DISCOVER_MS;
    default:
      return DEFAULT_MS;
  }
}
