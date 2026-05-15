import { z } from "zod";
import { withApplicationConnection } from "../../utils/ConnectionManager.js";
import type { DomainToolDefinition } from "../domainRuntime.js";

const GeometryActionSchema = z.enum([
  "create_line",
  "create_polyline",
  "create_3d_polyline",
  "create_text",
  "create_mtext",
]);

const canonicalInputShape = {
  action: GeometryActionSchema.describe("The geometry operation to perform."),
  startX: z.number().optional(),
  startY: z.number().optional(),
  startZ: z.number().optional(),
  endX: z.number().optional(),
  endY: z.number().optional(),
  endZ: z.number().optional(),
  vertices: z
    .array(z.object({ x: z.number(), y: z.number(), z: z.number().optional() }))
    .optional()
    .describe("Array of vertices for polyline creation."),
  closed: z.boolean().optional().describe("Whether the polyline is closed."),
  layer: z.string().optional().describe("Layer name."),
  text: z.string().optional().describe("Text content (for create_text/create_mtext)."),
  insertionX: z.number().optional().describe("Insertion point X."),
  insertionY: z.number().optional().describe("Insertion point Y."),
  height: z.number().optional().describe("Text height."),
  rotation: z.number().optional().describe("Rotation angle in degrees."),
};

export const GEOMETRY_DOMAIN_DEFINITION: DomainToolDefinition = {
  domain: "geometry",
  actions: {
    create_line: {
      action: "create_line",
      inputSchema: z.object({
        action: z.literal("create_line"),
        startX: z.number(),
        startY: z.number(),
        startZ: z.number().optional(),
        endX: z.number(),
        endY: z.number(),
        endZ: z.number().optional(),
        layer: z.string().optional(),
      }),
      capabilities: ["create"],
      requiresActiveDrawing: true,
      safeForRetry: false,
      pluginMethods: ["createLineSegment"],
      execute: async (args: any) =>
        await withApplicationConnection(async (c) =>
          await c.sendCommand("createLineSegment", {
            startX: args.startX,
            startY: args.startY,
            startZ: args.startZ,
            endX: args.endX,
            endY: args.endY,
            endZ: args.endZ,
            layer: args.layer,
          })
        ),
    },
    create_polyline: {
      action: "create_polyline",
      inputSchema: z.object({
        action: z.literal("create_polyline"),
        vertices: z.array(z.object({ x: z.number(), y: z.number() })),
        closed: z.boolean().optional(),
        layer: z.string().optional(),
      }),
      capabilities: ["create"],
      requiresActiveDrawing: true,
      safeForRetry: false,
      pluginMethods: ["createPolyline"],
      execute: async (args: any) =>
        await withApplicationConnection(async (c) =>
          await c.sendCommand("createPolyline", {
            vertices: args.vertices,
            closed: args.closed ?? false,
            layer: args.layer,
          })
        ),
    },
    create_3d_polyline: {
      action: "create_3d_polyline",
      inputSchema: z.object({
        action: z.literal("create_3d_polyline"),
        vertices: z.array(z.object({ x: z.number(), y: z.number(), z: z.number() })),
        closed: z.boolean().optional(),
        layer: z.string().optional(),
      }),
      capabilities: ["create"],
      requiresActiveDrawing: true,
      safeForRetry: false,
      pluginMethods: ["create3dPolyline"],
      execute: async (args: any) =>
        await withApplicationConnection(async (c) =>
          await c.sendCommand("create3dPolyline", {
            vertices: args.vertices,
            closed: args.closed ?? false,
            layer: args.layer,
          })
        ),
    },
    create_text: {
      action: "create_text",
      inputSchema: z.object({
        action: z.literal("create_text"),
        text: z.string(),
        insertionX: z.number(),
        insertionY: z.number(),
        height: z.number().optional(),
        rotation: z.number().optional(),
        layer: z.string().optional(),
      }),
      capabilities: ["create"],
      requiresActiveDrawing: true,
      safeForRetry: false,
      pluginMethods: ["createText"],
      execute: async (args: any) =>
        await withApplicationConnection(async (c) =>
          await c.sendCommand("createText", {
            text: args.text,
            insertionX: args.insertionX,
            insertionY: args.insertionY,
            height: args.height ?? 1.0,
            rotation: args.rotation ?? 0,
            layer: args.layer,
          })
        ),
    },
    create_mtext: {
      action: "create_mtext",
      inputSchema: z.object({
        action: z.literal("create_mtext"),
        text: z.string(),
        insertionX: z.number(),
        insertionY: z.number(),
        height: z.number().optional(),
        layer: z.string().optional(),
      }),
      capabilities: ["create"],
      requiresActiveDrawing: true,
      safeForRetry: false,
      pluginMethods: ["createMText"],
      execute: async (args: any) =>
        await withApplicationConnection(async (c) =>
          await c.sendCommand("createMText", {
            text: args.text,
            insertionX: args.insertionX,
            insertionY: args.insertionY,
            height: args.height ?? 1.0,
            layer: args.layer,
          })
        ),
    },
  },
  exposures: [
    {
      toolName: "civil3d_geometry",
      displayName: "Civil 3D Geometry",
      description:
        "Create basic AutoCAD geometry in Civil 3D. Actions: create_line (from start/end XYZ), " +
        "create_polyline (from 2D vertices), create_3d_polyline (from 3D vertices), " +
        "create_text, create_mtext.",
      inputShape: canonicalInputShape,
      supportedActions: [
        "create_line",
        "create_polyline",
        "create_3d_polyline",
        "create_text",
        "create_mtext",
      ],
      resolveAction: (rawArgs) => ({
        action: String(rawArgs.action),
        args: rawArgs,
      }),
    },
  ],
};
