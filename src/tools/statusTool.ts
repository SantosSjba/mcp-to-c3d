import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withApplicationConnection } from "../utils/ConnectionManager.js";
import { formatCivil3dError } from "../utils/civil3dError.js";
import { createLogger } from "../utils/logger.js";

const log = createLogger("StatusTool");

/**
 * civil3d_status — Real-time operation progress for long-running tasks.
 */
export function registerStatusTool(server: McpServer) {
  server.tool(
    "civil3d_status",
    "Get real-time status of the Civil 3D plugin operation queue and any in-progress task. " +
      "Use during long operations (corridor rebuild, surface build) to check elapsed time " +
      "without blocking the current operation.",
    {},
    async () => {
      try {
        const result = await withApplicationConnection(async (client) =>
          await client.sendCommand("getOperationStatus", {}, { timeoutMs: 10_000 })
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
        log.error("Status check failed", { error: message });
        return {
          content: [{ type: "text" as const, text: `Status check failed: ${message}` }],
          isError: true,
        };
      }
    }
  );
}
