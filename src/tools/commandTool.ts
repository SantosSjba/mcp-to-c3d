import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { z } from "zod";
import { withApplicationConnection } from "../utils/ConnectionManager.js";
import { formatCivil3dError } from "../utils/civil3dError.js";
import { createLogger } from "../utils/logger.js";

const log = createLogger("CommandTool");

/**
 * civil3d_command — Executes native Civil 3D / AutoCAD command strings.
 */
export function registerCommandTool(server: McpServer) {
  server.tool(
    "civil3d_command",
    "Execute a native Civil 3D or AutoCAD command string (same as typing in the command line). " +
      "Use for operations not easily done via C# API: CREATECORRIDOR, EXPORTLANDXML, etc. " +
      "Prefer civil3d_execute/civil3d_query when the C# API is sufficient.",
    {
      command: z
        .string()
        .describe(
          "Command string to execute, e.g. 'REGEN', 'CREATECORRIDOR', 'AeccCreateSurfaceGridFromDem'"
        ),
      waitForCompletion: z
        .boolean()
        .optional()
        .describe("Wait until the command finishes (default: true)"),
      timeoutMs: z
        .number()
        .optional()
        .describe("Max wait time in ms when waitForCompletion is true (default: 300000)"),
      description: z.string().optional().describe("Brief description for logging"),
    },
    async (args) => {
      try {
        log.info("Executing native command", {
          command: args.command,
          description: args.description,
        });

        const result = await withApplicationConnection(async (client) =>
          await client.sendCommand(
            "executeNativeCommand",
            {
              command: args.command,
              waitForCompletion: args.waitForCompletion ?? true,
              timeoutMs: args.timeoutMs,
            },
            { timeoutMs: args.timeoutMs }
          )
        );

        return {
          content: [
            {
              type: "text" as const,
              text: JSON.stringify(result, null, 2),
            },
          ],
        };
      } catch (error) {
        const message = formatCivil3dError(error);
        log.error("Native command failed", { error: message });
        return {
          content: [{ type: "text" as const, text: `Command failed: ${message}` }],
          isError: true,
        };
      }
    }
  );
}
