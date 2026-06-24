import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { z } from "zod";
import { withApplicationConnection } from "../utils/ConnectionManager.js";
import { formatCivil3dError } from "../utils/civil3dError.js";
import { createLogger } from "../utils/logger.js";

const log = createLogger("AuditTool");

/**
 * civil3d_audit — Read the audit log of operations executed by the AI.
 */
export function registerAuditTool(server: McpServer) {
  server.tool(
    "civil3d_audit",
    "Read the audit log of Civil 3D MCP operations. Shows timestamp, method, description, " +
      "success/failure, and duration for each operation. Useful for debugging and traceability.",
    {
      limit: z
        .number()
        .optional()
        .describe("Number of recent entries to return (default: 50, max: 500)"),
    },
    async (args) => {
      try {
        const result = await withApplicationConnection(async (client) =>
          await client.sendCommand("getAuditLog", { limit: args.limit ?? 50 })
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
        log.error("Audit read failed", { error: message });
        return {
          content: [{ type: "text" as const, text: `Audit read failed: ${message}` }],
          isError: true,
        };
      }
    }
  );
}
