import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { z } from "zod";
import { withApplicationConnection } from "../utils/ConnectionManager.js";
import { formatCivil3dError } from "../utils/civil3dError.js";
import { createLogger } from "../utils/logger.js";

const log = createLogger("ExecuteTool");

/**
 * civil3d_execute — Executes C# code in Civil 3D with WRITE access.
 * The code runs inside a transaction that gets committed on success.
 *
 * Available globals in the script:
 *   - Document / Doc (active AutoCAD document)
 *   - CivilDoc / Civil (active Civil 3D document)
 *   - Database (document database)
 *   - Transaction / Tr (active transaction — auto-committed)
 *   - Editor (document editor)
 *
 * Helper methods: GetSurfaceByName, GetAlignmentByName, GetProfileByName,
 * GetCogoPointByNumber, GetObjectIdByHandle, ListSurfaces, ListAlignments,
 * ListCogoPoints, ToRef, ToPoint
 *
 * The script can use all Civil 3D and AutoCAD namespaces (auto-imported).
 * Return a value to send it back as JSON to the AI.
 */
export function registerExecuteTool(server: McpServer) {
  server.tool(
    "civil3d_execute",
    "Execute C# code in Civil 3D with write access. The code runs inside a committed transaction. " +
      "Available globals: Document/Doc, CivilDoc/Civil, Database, Transaction/Tr, Editor. " +
      "Helpers: GetSurfaceByName, GetAlignmentByName, ListSurfaces, ListAlignments, ToRef, ToPoint. " +
      "All Civil 3D namespaces are auto-imported. Return a value to get results back as JSON. " +
      "Use civil3d_session for multi-step workflows. Use civil3d_command for native C3D commands.",
    {
      code: z.string().describe(
        "C# code to execute. Has access to Document, CivilDoc, Database, Transaction, Editor. " +
          "Example: var id = TinSurface.Create(Database, \"MySurface\"); return new { success = true };"
      ),
      description: z.string().optional().describe(
        "Brief description of what this code does (for logging/audit trail)."
      ),
      timeoutMs: z.number().optional().describe(
        "Max execution time in ms (default: 120000). Increase for heavy operations."
      ),
    },
    async (args) => {
      try {
        log.info("Executing write operation", { description: args.description });
        log.debug("Code", { code: args.code });

        const result = await withApplicationConnection(async (client) =>
          await client.sendCommand(
            "executeCode",
            {
              code: args.code,
              readOnly: false,
              description: args.description,
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
        log.error("Execute failed", { error: message });
        return {
          content: [{ type: "text" as const, text: `Execution failed: ${message}` }],
          isError: true,
        };
      }
    }
  );
}
