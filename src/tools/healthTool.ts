import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withApplicationConnection } from "../utils/ConnectionManager.js";
import { formatCivil3dError } from "../utils/civil3dError.js";
import { createLogger } from "../utils/logger.js";

const log = createLogger("HealthTool");

/**
 * civil3d_health — Reports plugin status, Civil 3D version, and active drawing.
 * Use this before other tools to verify the connection is alive.
 */
export function registerHealthTool(server: McpServer) {
  server.tool(
    "civil3d_health",
    "Check Civil 3D MCP connection health. Returns plugin status, Civil 3D version, " +
      "active drawing info, and queue state. Call this first to verify Civil 3D is " +
      "running and the plugin is loaded.",
    {},
    async () => {
      try {
        log.debug("Health check requested");

        const result = await withApplicationConnection(async (client) =>
          await client.sendCommand("getCivil3DHealth", {})
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
        log.error("Health check failed", { error: message });
        return {
          content: [{ type: "text" as const, text: `Health check failed: ${message}` }],
          isError: true,
        };
      }
    }
  );
}
