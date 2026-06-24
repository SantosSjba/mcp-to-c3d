import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { z } from "zod";
import { withApplicationConnection } from "../utils/ConnectionManager.js";
import { formatCivil3dError } from "../utils/civil3dError.js";
import { createLogger } from "../utils/logger.js";

const log = createLogger("QueryTool");

/**
 * civil3d_query — Executes C# code in Civil 3D in READ-ONLY mode.
 * The transaction is NOT committed — no changes are persisted.
 *
 * Same globals as civil3d_execute but safe for data retrieval.
 * Use this for listing objects, getting properties, analyzing data, etc.
 */
export function registerQueryTool(server: McpServer) {
  server.tool(
    "civil3d_query",
    "Execute C# code in Civil 3D in READ-ONLY mode (no changes saved). " +
      "Available globals: Document/Doc, CivilDoc/Civil, Database, Transaction/Tr, Editor. " +
      "Helpers: GetSurfaceByName, GetAlignmentByName, ListSurfaces, ListAlignments, ListCogoPoints. " +
      "All Civil 3D namespaces are auto-imported. Return a value to get results as JSON. " +
      "Use this for querying data: listing objects, getting properties, analyzing surfaces, etc.",
    {
      code: z.string().describe(
        "C# code to query data. Has access to Document, CivilDoc, Database, Transaction, Editor. " +
          "Example: var surfaces = new List<object>(); " +
          "foreach (ObjectId id in CivilDoc.GetSurfaceIds()) { " +
          "var s = Transaction.GetObject(id, OpenMode.ForRead) as TinSurface; " +
          'surfaces.Add(new { s.Name, s.Layer }); } return surfaces;'
      ),
      timeoutMs: z.number().optional().describe(
        "Max execution time in ms (default: 120000)"
      ),
    },
    async (args) => {
      try {
        log.debug("Executing query", { code: args.code });

        const result = await withApplicationConnection(async (client) =>
          await client.sendCommand(
            "executeCode",
            {
              code: args.code,
              readOnly: true,
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
        log.error("Query failed", { error: message });
        return {
          content: [{ type: "text" as const, text: `Query failed: ${message}` }],
          isError: true,
        };
      }
    }
  );
}
