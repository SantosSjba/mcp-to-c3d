import { z, type ZodRawShape, type ZodTypeAny } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import type { ToolCapability, ToolCatalogEntry, ToolDomain } from "./toolMetadata.js";
import { captureToolHandler } from "./toolHandlerRegistry.js";

type JsonObject = Record<string, unknown>;

/**
 * Defines a single action within a domain (e.g. "list", "get", "create").
 */
export interface DomainActionDefinition<TArgs = JsonObject> {
  action: string;
  inputSchema: z.ZodType<TArgs>;
  execute: (args: TArgs) => Promise<unknown>;
  responseSchema?: ZodTypeAny;
  capabilities: ToolCapability[];
  requiresActiveDrawing: boolean;
  safeForRetry: boolean;
  pluginMethods?: string[];
}

/**
 * Defines how a domain tool is exposed to the MCP client.
 * A single exposure becomes one MCP tool with multiple actions.
 */
export interface DomainToolExposure {
  toolName: string;
  displayName: string;
  description: string;
  inputShape: ZodRawShape;
  supportedActions: string[];
  resolveAction: (rawArgs: JsonObject) => { action: string; args: JsonObject };
  capabilities?: ToolCapability[];
  operations?: string[];
  pluginMethods?: string[];
  requiresActiveDrawing?: boolean;
  safeForRetry?: boolean;
  status?: "implemented" | "planned";
}

/**
 * Full definition of a domain — its actions and how they're exposed as tools.
 */
export interface DomainToolDefinition {
  domain: ToolDomain;
  actions: Record<string, DomainActionDefinition>;
  exposures: DomainToolExposure[];
}

function uniqueStrings(values: Iterable<string | undefined>): string[] | undefined {
  const unique = [...new Set([...values].filter((v): v is string => Boolean(v)))];
  return unique.length > 0 ? unique : undefined;
}

function uniqueCapabilities(values: Iterable<ToolCapability | undefined>): ToolCapability[] {
  return [...new Set([...values].filter((v): v is ToolCapability => Boolean(v)))];
}

function buildToolErrorResult(toolName: string, actionName: string | undefined, error: unknown) {
  const message = error instanceof Error ? error.message : String(error);
  const scopedName = actionName ? `${toolName} action '${actionName}'` : toolName;

  console.error(`Error in ${scopedName}:`, error);

  return {
    content: [
      {
        type: "text" as const,
        text: `${scopedName} failed: ${message}`,
      },
    ],
    isError: true,
  };
}

/**
 * Execute a domain tool exposure with the given raw args.
 */
async function executeExposure(
  definition: DomainToolDefinition,
  exposure: DomainToolExposure,
  rawArgs: JsonObject
) {
  const resolved = exposure.resolveAction(rawArgs);
  const actionName = resolved.action;

  if (!exposure.supportedActions.includes(actionName)) {
    throw new Error(
      `Unsupported action '${actionName}' for tool '${exposure.toolName}'. ` +
        `Supported actions: ${exposure.supportedActions.join(", ")}.`
    );
  }

  const actionDefinition = definition.actions[actionName];
  if (!actionDefinition) {
    throw new Error(`Action '${actionName}' is not defined for domain '${definition.domain}'.`);
  }

  const parsedArgs = actionDefinition.inputSchema.parse(resolved.args);
  const response = await actionDefinition.execute(parsedArgs);
  const validatedResponse = actionDefinition.responseSchema
    ? actionDefinition.responseSchema.parse(response)
    : response;

  return {
    content: [
      {
        type: "text" as const,
        text: JSON.stringify(validatedResponse, null, 2),
      },
    ],
  };
}

/**
 * Register all tool exposures from a domain definition with the MCP server.
 */
export function registerDomainTools(server: McpServer, definition: DomainToolDefinition) {
  for (const exposure of definition.exposures) {
    const handler = async (rawArgs: Record<string, unknown>) => {
      try {
        return await executeExposure(definition, exposure, rawArgs as JsonObject);
      } catch (error) {
        const actionName =
          typeof (rawArgs as JsonObject).action === "string"
            ? String((rawArgs as JsonObject).action)
            : exposure.supportedActions.length === 1
              ? exposure.supportedActions[0]
              : undefined;

        return buildToolErrorResult(exposure.toolName, actionName, error);
      }
    };

    server.tool(exposure.toolName, exposure.description, exposure.inputShape, handler);
    captureToolHandler(exposure.toolName, handler);
  }
}

/**
 * Build catalog entries from a domain definition (for documentation/introspection).
 */
export function buildDomainToolCatalogEntries(
  definition: DomainToolDefinition
): ToolCatalogEntry[] {
  return definition.exposures.map((exposure) => {
    const supportedActionDefs = exposure.supportedActions
      .map((a) => definition.actions[a])
      .filter((a): a is DomainActionDefinition => Boolean(a));

    return {
      toolName: exposure.toolName,
      displayName: exposure.displayName,
      description: exposure.description,
      domain: definition.domain,
      capabilities:
        exposure.capabilities ??
        uniqueCapabilities(supportedActionDefs.flatMap((a) => a.capabilities)),
      operations:
        exposure.operations ??
        (exposure.supportedActions.length > 1 ? exposure.supportedActions : undefined),
      pluginMethods:
        exposure.pluginMethods ??
        uniqueStrings(supportedActionDefs.flatMap((a) => a.pluginMethods ?? [])),
      requiresActiveDrawing:
        exposure.requiresActiveDrawing ??
        supportedActionDefs.some((a) => a.requiresActiveDrawing),
      safeForRetry:
        exposure.safeForRetry ?? supportedActionDefs.every((a) => a.safeForRetry),
      status: exposure.status ?? "implemented",
    };
  });
}
