import { z } from "zod";
import { withApplicationConnection } from "../../utils/ConnectionManager.js";
import type { DomainToolDefinition } from "../domainRuntime.js";

const PointActionSchema = z.enum([
  "list",
  "get",
  "create",
  "delete",
  "list_groups",
  "import",
]);

const canonicalInputShape = {
  action: PointActionSchema.describe("The point operation to perform."),
  pointNumber: z.number().optional().describe("Point number (for get/delete)."),
  easting: z.number().optional().describe("Easting (X) coordinate."),
  northing: z.number().optional().describe("Northing (Y) coordinate."),
  elevation: z.number().optional().describe("Elevation (Z) coordinate."),
  rawDescription: z.string().optional().describe("Raw description for the point."),
  points: z
    .array(
      z.object({
        easting: z.number(),
        northing: z.number(),
        elevation: z.number().optional(),
        rawDescription: z.string().optional(),
      })
    )
    .optional()
    .describe("Array of points (for batch create)."),
  groupName: z.string().optional().describe("Point group name."),
  filePath: z.string().optional().describe("File path for import."),
  format: z.string().optional().describe("Import format (e.g. PNEZD)."),
  limit: z.number().optional().describe("Max points to return."),
};

export const POINT_DOMAIN_DEFINITION: DomainToolDefinition = {
  domain: "point",
  actions: {
    list: {
      action: "list",
      inputSchema: z.object({
        action: z.literal("list"),
        groupName: z.string().optional(),
        limit: z.number().optional(),
      }),
      capabilities: ["query"],
      requiresActiveDrawing: true,
      safeForRetry: true,
      pluginMethods: ["listCogoPoints"],
      execute: async (args: any) =>
        await withApplicationConnection(async (c) =>
          await c.sendCommand("listCogoPoints", {
            groupName: args.groupName,
            limit: args.limit ?? 500,
          })
        ),
    },
    get: {
      action: "get",
      inputSchema: z.object({
        action: z.literal("get"),
        pointNumber: z.number(),
      }),
      capabilities: ["query", "inspect"],
      requiresActiveDrawing: true,
      safeForRetry: true,
      pluginMethods: ["getCogoPoint"],
      execute: async (args: any) =>
        await withApplicationConnection(async (c) =>
          await c.sendCommand("getCogoPoint", { pointNumber: args.pointNumber })
        ),
    },
    create: {
      action: "create",
      inputSchema: z.object({
        action: z.literal("create"),
        points: z.array(
          z.object({
            easting: z.number(),
            northing: z.number(),
            elevation: z.number().optional(),
            rawDescription: z.string().optional(),
          })
        ),
        groupName: z.string().optional(),
      }),
      capabilities: ["create"],
      requiresActiveDrawing: true,
      safeForRetry: false,
      pluginMethods: ["createCogoPoints"],
      execute: async (args: any) =>
        await withApplicationConnection(async (c) =>
          await c.sendCommand("createCogoPoints", {
            points: args.points,
            groupName: args.groupName,
          })
        ),
    },
    delete: {
      action: "delete",
      inputSchema: z.object({
        action: z.literal("delete"),
        pointNumber: z.number(),
      }),
      capabilities: ["delete"],
      requiresActiveDrawing: true,
      safeForRetry: false,
      pluginMethods: ["deleteCogoPoints"],
      execute: async (args: any) =>
        await withApplicationConnection(async (c) =>
          await c.sendCommand("deleteCogoPoints", {
            pointNumbers: [args.pointNumber],
          })
        ),
    },
    list_groups: {
      action: "list_groups",
      inputSchema: z.object({ action: z.literal("list_groups") }),
      capabilities: ["query"],
      requiresActiveDrawing: true,
      safeForRetry: true,
      pluginMethods: ["listPointGroups"],
      execute: async () =>
        await withApplicationConnection(async (c) =>
          await c.sendCommand("listPointGroups", {})
        ),
    },
    import: {
      action: "import",
      inputSchema: z.object({
        action: z.literal("import"),
        filePath: z.string(),
        format: z.string().optional(),
        groupName: z.string().optional(),
      }),
      capabilities: ["import", "create"],
      requiresActiveDrawing: true,
      safeForRetry: false,
      pluginMethods: ["importCogoPoints"],
      execute: async (args: any) =>
        await withApplicationConnection(async (c) =>
          await c.sendCommand("importCogoPoints", {
            filePath: args.filePath,
            format: args.format ?? "PNEZD",
            groupName: args.groupName,
          })
        ),
    },
  },
  exposures: [
    {
      toolName: "civil3d_point",
      displayName: "Civil 3D COGO Points",
      description:
        "Manage COGO (Coordinate Geometry) points. Actions: list (optionally by group), " +
        "get (by point number), create (single or batch), delete, list_groups, " +
        "import (from file).",
      inputShape: canonicalInputShape,
      supportedActions: ["list", "get", "create", "delete", "list_groups", "import"],
      resolveAction: (rawArgs) => ({
        action: String(rawArgs.action),
        args: rawArgs,
      }),
    },
  ],
};
