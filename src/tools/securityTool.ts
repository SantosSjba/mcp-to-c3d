import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withApplicationConnection } from "../utils/ConnectionManager.js";
import { formatCivil3dError } from "../utils/civil3dError.js";
import { createLogger } from "../utils/logger.js";

const log = createLogger("SecurityTool");

/**
 * civil3d_security — Read the active security policy (sandbox mode, export paths, confirmation).
 */
export function registerSecurityTool(server: McpServer) {
  server.tool(
    "civil3d_security",
    "Read the Civil 3D MCP security policy: sandbox mode (strict/professional/unlocked), " +
      "allowed file export paths, and whether destructive operations require confirmed: true. " +
      "Call this before file exports or destructive writes.",
    {},
    async () => {
      try {
        const result = await withApplicationConnection(async (client) =>
          await client.sendCommand("getSecurityPolicy", {})
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
        log.error("Security policy read failed", { error: message });
        return {
          content: [{ type: "text" as const, text: `Security policy read failed: ${message}` }],
          isError: true,
        };
      }
    }
  );
}
