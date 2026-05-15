/**
 * Global registry that captures MCP tool handlers during server.tool() registration.
 * Enables the HTTP bridge (optional) to invoke any registered tool without going
 * through the MCP stdio protocol.
 */

/** MCP CallToolResult shape returned by every server.tool() handler */
interface McpCallToolResult {
  content: Array<{ type: string; text?: string }>;
  isError?: boolean;
}

type ToolHandler = (
  args: Record<string, unknown>,
  extra?: unknown
) => Promise<McpCallToolResult>;

const _handlers = new Map<string, ToolHandler>();

/** Store a tool handler during registration. */
export function captureToolHandler(name: string, handler: ToolHandler): void {
  _handlers.set(name, handler);
}

/** Retrieve a previously captured tool handler by name. */
export function getToolHandler(name: string): ToolHandler | undefined {
  return _handlers.get(name);
}

/** Check whether a handler exists for the given tool name. */
export function hasToolHandler(name: string): boolean {
  return _handlers.has(name);
}

/** Return all registered tool names (sorted). */
export function listRegisteredToolNames(): string[] {
  return [..._handlers.keys()].sort();
}

/**
 * Execute a registered tool and unwrap the MCP CallToolResult into a plain
 * JSON-serializable object suitable for HTTP responses.
 */
export async function executeRegisteredTool(
  toolName: string,
  parameters: Record<string, unknown>
): Promise<unknown> {
  const handler = _handlers.get(toolName);
  if (!handler) {
    throw new Error(
      `Tool '${toolName}' is not registered. Available: ${listRegisteredToolNames().length} tools.`
    );
  }

  const mcpResult = await handler(parameters, {});

  if (mcpResult?.isError) {
    const errorText =
      mcpResult.content?.[0]?.text ?? `Tool '${toolName}' returned an error`;
    throw new Error(errorText);
  }

  const text = mcpResult?.content?.[0]?.text;
  if (text) {
    try {
      return JSON.parse(text);
    } catch {
      return { message: text };
    }
  }

  return mcpResult;
}
