import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { z } from "zod";
import { withApplicationConnection } from "../utils/ConnectionManager.js";
import { formatCivil3dError } from "../utils/civil3dError.js";
import { createLogger } from "../utils/logger.js";

const log = createLogger("SessionTool");

/**
 * civil3d_session — Multi-step transactions with explicit commit/abort.
 */
export function registerSessionTool(server: McpServer) {
  server.tool(
    "civil3d_session",
    "Manage multi-step Civil 3D script sessions with a shared transaction. " +
      "Use 'begin' to start, 'execute' for each script step (no commit), " +
      "'commit' to save all changes, or 'abort' to discard. " +
      "Useful for complex workflows that need multiple code steps in one transaction.",
    {
      action: z
        .enum(["begin", "execute", "commit", "abort"])
        .describe("begin | execute | commit | abort"),
      sessionId: z
        .string()
        .optional()
        .describe("Session ID (required for execute, commit, abort)"),
      code: z
        .string()
        .optional()
        .describe("C# code to run (required for execute action)"),
      description: z.string().optional().describe("Brief description for logging"),
    },
    async (args) => {
      try {
        switch (args.action) {
          case "begin": {
            const result = await withApplicationConnection(async (client) =>
              await client.sendCommand("beginSession", {})
            );
            return {
              content: [{ type: "text" as const, text: JSON.stringify(result, null, 2) }],
            };
          }

          case "execute": {
            if (!args.sessionId || !args.code) {
              return {
                content: [
                  {
                    type: "text" as const,
                    text: "Parameters 'sessionId' and 'code' are required for execute.",
                  },
                ],
                isError: true,
              };
            }

            log.info("Session execute", { sessionId: args.sessionId, description: args.description });

            const result = await withApplicationConnection(async (client) =>
              await client.sendCommand("sessionExecute", {
                sessionId: args.sessionId,
                code: args.code,
                description: args.description,
              })
            );

            return {
              content: [{ type: "text" as const, text: JSON.stringify(result, null, 2) }],
            };
          }

          case "commit": {
            if (!args.sessionId) {
              return {
                content: [
                  { type: "text" as const, text: "Parameter 'sessionId' is required for commit." },
                ],
                isError: true,
              };
            }

            const result = await withApplicationConnection(async (client) =>
              await client.sendCommand("sessionCommit", { sessionId: args.sessionId })
            );

            return {
              content: [{ type: "text" as const, text: JSON.stringify(result, null, 2) }],
            };
          }

          case "abort": {
            if (!args.sessionId) {
              return {
                content: [
                  { type: "text" as const, text: "Parameter 'sessionId' is required for abort." },
                ],
                isError: true,
              };
            }

            const result = await withApplicationConnection(async (client) =>
              await client.sendCommand("sessionAbort", { sessionId: args.sessionId })
            );

            return {
              content: [{ type: "text" as const, text: JSON.stringify(result, null, 2) }],
            };
          }
        }
      } catch (error) {
        const message = formatCivil3dError(error);
        log.error("Session operation failed", { action: args.action, error: message });
        return {
          content: [{ type: "text" as const, text: `Session ${args.action} failed: ${message}` }],
          isError: true,
        };
      }
    }
  );
}
