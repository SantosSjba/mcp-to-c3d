import { z } from "zod";
import { withApplicationConnection } from "../../utils/ConnectionManager.js";
import type { DomainToolDefinition } from "../domainRuntime.js";

const AlignmentActionSchema = z.enum([
  "list",
  "get",
  "create",
  "delete",
  "station_to_point",
  "point_to_station",
]);

const canonicalInputShape = {
  action: AlignmentActionSchema.describe("The alignment operation to perform."),
  name: z.string().optional().describe("Alignment name."),
  station: z.number().optional().describe("Station value along the alignment."),
  offset: z.number().optional().describe("Offset from the alignment."),
  x: z.number().optional().describe("X coordinate (for point_to_station)."),
  y: z.number().optional().describe("Y coordinate (for point_to_station)."),
  polylineHandle: z.string().optional().describe("Handle of an existing polyline to create alignment from."),
  style: z.string().optional().describe("Alignment style name."),
  layer: z.string().optional().describe("Layer name."),
};

export const ALIGNMENT_DOMAIN_DEFINITION: DomainToolDefinition = {
  domain: "alignment",
  actions: {
    list: {
      action: "list",
      inputSchema: z.object({ action: z.literal("list") }),
      capabilities: ["query"],
      requiresActiveDrawing: true,
      safeForRetry: true,
      pluginMethods: ["listAlignments"],
      execute: async () =>
        await withApplicationConnection(async (c) =>
          await c.sendCommand("listAlignments", {})
        ),
    },
    get: {
      action: "get",
      inputSchema: z.object({ action: z.literal("get"), name: z.string() }),
      capabilities: ["query", "inspect"],
      requiresActiveDrawing: true,
      safeForRetry: true,
      pluginMethods: ["getAlignment"],
      execute: async (args: any) =>
        await withApplicationConnection(async (c) =>
          await c.sendCommand("getAlignment", { name: args.name })
        ),
    },
    create: {
      action: "create",
      inputSchema: z.object({
        action: z.literal("create"),
        name: z.string(),
        polylineHandle: z.string().optional(),
        style: z.string().optional(),
        layer: z.string().optional(),
      }),
      capabilities: ["create"],
      requiresActiveDrawing: true,
      safeForRetry: false,
      pluginMethods: ["createAlignment"],
      execute: async (args: any) =>
        await withApplicationConnection(async (c) =>
          await c.sendCommand("createAlignment", {
            name: args.name,
            polylineHandle: args.polylineHandle,
            style: args.style,
            layer: args.layer,
          })
        ),
    },
    delete: {
      action: "delete",
      inputSchema: z.object({
        action: z.literal("delete"),
        name: z.string(),
      }),
      capabilities: ["delete"],
      requiresActiveDrawing: true,
      safeForRetry: false,
      pluginMethods: ["deleteAlignment"],
      execute: async (args: any) =>
        await withApplicationConnection(async (c) =>
          await c.sendCommand("deleteAlignment", { name: args.name })
        ),
    },
    station_to_point: {
      action: "station_to_point",
      inputSchema: z.object({
        action: z.literal("station_to_point"),
        name: z.string(),
        station: z.number(),
        offset: z.number().optional(),
      }),
      capabilities: ["query", "analyze"],
      requiresActiveDrawing: true,
      safeForRetry: true,
      pluginMethods: ["alignmentStationToPoint"],
      execute: async (args: any) =>
        await withApplicationConnection(async (c) =>
          await c.sendCommand("alignmentStationToPoint", {
            name: args.name,
            station: args.station,
            offset: args.offset ?? 0,
          })
        ),
    },
    point_to_station: {
      action: "point_to_station",
      inputSchema: z.object({
        action: z.literal("point_to_station"),
        name: z.string(),
        x: z.number(),
        y: z.number(),
      }),
      capabilities: ["query", "analyze"],
      requiresActiveDrawing: true,
      safeForRetry: true,
      pluginMethods: ["alignmentPointToStation"],
      execute: async (args: any) =>
        await withApplicationConnection(async (c) =>
          await c.sendCommand("alignmentPointToStation", {
            name: args.name,
            x: args.x,
            y: args.y,
          })
        ),
    },
  },
  exposures: [
    {
      toolName: "civil3d_alignment",
      displayName: "Civil 3D Alignment",
      description:
        "Manage Civil 3D alignments (horizontal geometry). Actions: list, get (by name), " +
        "create (from polyline), delete, station_to_point (convert station+offset to X,Y), " +
        "point_to_station (convert X,Y to station+offset).",
      inputShape: canonicalInputShape,
      supportedActions: [
        "list",
        "get",
        "create",
        "delete",
        "station_to_point",
        "point_to_station",
      ],
      resolveAction: (rawArgs) => ({
        action: String(rawArgs.action),
        args: rawArgs,
      }),
    },
  ],
};
