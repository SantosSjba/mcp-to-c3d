import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { z } from "zod";
import { withApplicationConnection } from "../utils/ConnectionManager.js";
import { formatCivil3dError } from "../utils/civil3dError.js";
import { createLogger } from "../utils/logger.js";

const log = createLogger("DiscoverTool");

const DISCOVER_CATEGORIES = [
  "summary",
  "surfaces",
  "alignments",
  "profiles",
  "corridors",
  "pipeNetworks",
  "sites",
  "parcels",
  "cogoPoints",
  "sampleLines",
  "styles",
] as const;

/**
 * civil3d_discover — Inventory of Civil 3D drawing objects without writing C#.
 */
export function registerDiscoverTool(server: McpServer) {
  server.tool(
    "civil3d_discover",
    "Discover and inventory Civil 3D drawing objects without writing C# code. " +
      "Returns structured JSON for surfaces, alignments, profiles, corridors, " +
      "pipe networks, sites, parcels, COGO points, sample lines, and styles. " +
      "Use this FIRST to understand what exists in the drawing before executing code.",
    {
      categories: z
        .array(z.enum(DISCOVER_CATEGORIES))
        .optional()
        .describe(
          "Categories to include. Default: all. Options: " + DISCOVER_CATEGORIES.join(", ")
        ),
      limit: z
        .number()
        .optional()
        .describe("Max items per category (default: 100)"),
      timeoutMs: z.number().optional().describe("Max time in ms (default: 60000)"),
    },
    async (args) => {
      try {
        log.info("Discovering drawing", { categories: args.categories, limit: args.limit });

        const result = await withApplicationConnection(async (client) =>
          await client.sendCommand(
            "discoverDrawing",
            {
              categories: args.categories,
              limit: args.limit ?? 100,
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
        log.error("Discover failed", { error: message });
        return {
          content: [{ type: "text" as const, text: `Discover failed: ${message}` }],
          isError: true,
        };
      }
    }
  );
}
