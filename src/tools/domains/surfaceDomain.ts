import { z } from "zod";
import { withApplicationConnection } from "../../utils/ConnectionManager.js";
import type { DomainToolDefinition } from "../domainRuntime.js";

const Point2DSchema = z.object({ x: z.number(), y: z.number() });
const Point3DSchema = z.object({ x: z.number(), y: z.number(), z: z.number() });

const SurfaceActionSchema = z.enum([
  "list",
  "get",
  "get_elevation",
  "get_statistics",
  "create",
  "delete",
  "add_points",
  "add_breakline",
  "add_boundary",
  "extract_contours",
  "compute_volume",
]);

const canonicalInputShape = {
  action: SurfaceActionSchema.describe("The surface operation to perform."),
  name: z.string().optional().describe("Surface name."),
  x: z.number().optional().describe("X coordinate (for get_elevation)."),
  y: z.number().optional().describe("Y coordinate (for get_elevation)."),
  points: z.array(Point3DSchema).optional().describe("Array of 3D points."),
  style: z.string().optional().describe("Surface style name."),
  layer: z.string().optional().describe("Layer name."),
  description: z.string().optional().describe("Description text."),
  breaklineType: z.enum(["standard", "wall", "proximity"]).optional(),
  boundaryType: z.enum(["show", "hide", "outer", "data_clip"]).optional(),
  boundaryPoints: z.array(Point2DSchema).optional().describe("Boundary polygon points."),
  minorInterval: z.number().optional().describe("Minor contour interval."),
  majorInterval: z.number().optional().describe("Major contour interval."),
  baseSurface: z.string().optional().describe("Base surface for volume calculation."),
  comparisonSurface: z.string().optional().describe("Comparison surface for volume calculation."),
};

export const SURFACE_DOMAIN_DEFINITION: DomainToolDefinition = {
  domain: "surface",
  actions: {
    list: {
      action: "list",
      inputSchema: z.object({ action: z.literal("list") }),
      capabilities: ["query"],
      requiresActiveDrawing: true,
      safeForRetry: true,
      pluginMethods: ["listSurfaces"],
      execute: async () =>
        await withApplicationConnection(async (c) =>
          await c.sendCommand("listSurfaces", {})
        ),
    },
    get: {
      action: "get",
      inputSchema: z.object({ action: z.literal("get"), name: z.string() }),
      capabilities: ["query", "inspect"],
      requiresActiveDrawing: true,
      safeForRetry: true,
      pluginMethods: ["getSurface"],
      execute: async (args: any) =>
        await withApplicationConnection(async (c) =>
          await c.sendCommand("getSurface", { name: args.name })
        ),
    },
    get_elevation: {
      action: "get_elevation",
      inputSchema: z.object({
        action: z.literal("get_elevation"),
        name: z.string(),
        x: z.number(),
        y: z.number(),
      }),
      capabilities: ["query", "analyze"],
      requiresActiveDrawing: true,
      safeForRetry: true,
      pluginMethods: ["getSurfaceElevation"],
      execute: async (args: any) =>
        await withApplicationConnection(async (c) =>
          await c.sendCommand("getSurfaceElevation", {
            name: args.name,
            x: args.x,
            y: args.y,
          })
        ),
    },
    get_statistics: {
      action: "get_statistics",
      inputSchema: z.object({
        action: z.literal("get_statistics"),
        name: z.string(),
      }),
      capabilities: ["query", "inspect"],
      requiresActiveDrawing: true,
      safeForRetry: true,
      pluginMethods: ["getSurfaceStatistics"],
      execute: async (args: any) =>
        await withApplicationConnection(async (c) =>
          await c.sendCommand("getSurfaceStatistics", { name: args.name })
        ),
    },
    create: {
      action: "create",
      inputSchema: z.object({
        action: z.literal("create"),
        name: z.string(),
        style: z.string().optional(),
        layer: z.string().optional(),
        description: z.string().optional(),
      }),
      capabilities: ["create"],
      requiresActiveDrawing: true,
      safeForRetry: false,
      pluginMethods: ["createSurface"],
      execute: async (args: any) =>
        await withApplicationConnection(async (c) =>
          await c.sendCommand("createSurface", {
            name: args.name,
            style: args.style,
            layer: args.layer,
            description: args.description,
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
      pluginMethods: ["deleteSurface"],
      execute: async (args: any) =>
        await withApplicationConnection(async (c) =>
          await c.sendCommand("deleteSurface", { name: args.name })
        ),
    },
    add_points: {
      action: "add_points",
      inputSchema: z.object({
        action: z.literal("add_points"),
        name: z.string(),
        points: z.array(Point3DSchema),
        description: z.string().optional(),
      }),
      capabilities: ["edit"],
      requiresActiveDrawing: true,
      safeForRetry: false,
      pluginMethods: ["addSurfacePoints"],
      execute: async (args: any) =>
        await withApplicationConnection(async (c) =>
          await c.sendCommand("addSurfacePoints", {
            name: args.name,
            points: args.points,
            description: args.description,
          })
        ),
    },
    add_breakline: {
      action: "add_breakline",
      inputSchema: z.object({
        action: z.literal("add_breakline"),
        name: z.string(),
        breaklineType: z.enum(["standard", "wall", "proximity"]),
        points: z.array(Point3DSchema),
        description: z.string().optional(),
      }),
      capabilities: ["edit"],
      requiresActiveDrawing: true,
      safeForRetry: false,
      pluginMethods: ["addSurfaceBreakline"],
      execute: async (args: any) =>
        await withApplicationConnection(async (c) =>
          await c.sendCommand("addSurfaceBreakline", {
            name: args.name,
            breaklineType: args.breaklineType,
            points: args.points,
            description: args.description,
          })
        ),
    },
    add_boundary: {
      action: "add_boundary",
      inputSchema: z.object({
        action: z.literal("add_boundary"),
        name: z.string(),
        boundaryType: z.enum(["show", "hide", "outer", "data_clip"]),
        boundaryPoints: z.array(Point2DSchema),
      }),
      capabilities: ["edit"],
      requiresActiveDrawing: true,
      safeForRetry: false,
      pluginMethods: ["addSurfaceBoundary"],
      execute: async (args: any) =>
        await withApplicationConnection(async (c) =>
          await c.sendCommand("addSurfaceBoundary", {
            name: args.name,
            boundaryType: args.boundaryType,
            points: args.boundaryPoints,
          })
        ),
    },
    extract_contours: {
      action: "extract_contours",
      inputSchema: z.object({
        action: z.literal("extract_contours"),
        name: z.string(),
        minorInterval: z.number(),
        majorInterval: z.number(),
      }),
      capabilities: ["generate"],
      requiresActiveDrawing: true,
      safeForRetry: false,
      pluginMethods: ["extractSurfaceContours"],
      execute: async (args: any) =>
        await withApplicationConnection(async (c) =>
          await c.sendCommand("extractSurfaceContours", {
            name: args.name,
            minorInterval: args.minorInterval,
            majorInterval: args.majorInterval,
          })
        ),
    },
    compute_volume: {
      action: "compute_volume",
      inputSchema: z.object({
        action: z.literal("compute_volume"),
        baseSurface: z.string(),
        comparisonSurface: z.string(),
      }),
      capabilities: ["query", "analyze"],
      requiresActiveDrawing: true,
      safeForRetry: true,
      pluginMethods: ["computeSurfaceVolume"],
      execute: async (args: any) =>
        await withApplicationConnection(async (c) =>
          await c.sendCommand("computeSurfaceVolume", {
            baseSurface: args.baseSurface,
            comparisonSurface: args.comparisonSurface,
          })
        ),
    },
  },
  exposures: [
    {
      toolName: "civil3d_surface",
      displayName: "Civil 3D Surface",
      description:
        "Manage Civil 3D surfaces. Actions: list, get (by name), get_elevation (at X,Y), " +
        "get_statistics, create, delete, add_points, add_breakline, add_boundary, " +
        "extract_contours, compute_volume (between two surfaces).",
      inputShape: canonicalInputShape,
      supportedActions: [
        "list",
        "get",
        "get_elevation",
        "get_statistics",
        "create",
        "delete",
        "add_points",
        "add_breakline",
        "add_boundary",
        "extract_contours",
        "compute_volume",
      ],
      resolveAction: (rawArgs) => ({
        action: String(rawArgs.action),
        args: rawArgs,
      }),
    },
  ],
};
