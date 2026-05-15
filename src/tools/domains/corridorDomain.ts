import { z } from "zod";
import { withApplicationConnection } from "../../utils/ConnectionManager.js";
import type { DomainToolDefinition } from "../domainRuntime.js";

const CorridorActionSchema = z.enum([
  "list",
  "get",
  "rebuild",
  "get_surfaces",
  "get_feature_lines",
  "compute_volumes",
]);

const canonicalInputShape = {
  action: CorridorActionSchema.describe("The corridor operation to perform."),
  name: z.string().optional().describe("Corridor name."),
};

export const CORRIDOR_DOMAIN_DEFINITION: DomainToolDefinition = {
  domain: "corridor",
  actions: {
    list: {
      action: "list",
      inputSchema: z.object({ action: z.literal("list") }),
      capabilities: ["query"],
      requiresActiveDrawing: true,
      safeForRetry: true,
      pluginMethods: ["listCorridors"],
      execute: async () =>
        await withApplicationConnection(async (c) =>
          await c.sendCommand("listCorridors", {})
        ),
    },
    get: {
      action: "get",
      inputSchema: z.object({ action: z.literal("get"), name: z.string() }),
      capabilities: ["query", "inspect"],
      requiresActiveDrawing: true,
      safeForRetry: true,
      pluginMethods: ["getCorridor"],
      execute: async (args: any) =>
        await withApplicationConnection(async (c) =>
          await c.sendCommand("getCorridor", { name: args.name })
        ),
    },
    rebuild: {
      action: "rebuild",
      inputSchema: z.object({ action: z.literal("rebuild"), name: z.string() }),
      capabilities: ["manage"],
      requiresActiveDrawing: true,
      safeForRetry: false,
      pluginMethods: ["rebuildCorridor"],
      execute: async (args: any) =>
        await withApplicationConnection(async (c) =>
          await c.sendCommand("rebuildCorridor", { name: args.name })
        ),
    },
    get_surfaces: {
      action: "get_surfaces",
      inputSchema: z.object({ action: z.literal("get_surfaces"), name: z.string() }),
      capabilities: ["query"],
      requiresActiveDrawing: true,
      safeForRetry: true,
      pluginMethods: ["getCorridorSurfaces"],
      execute: async (args: any) =>
        await withApplicationConnection(async (c) =>
          await c.sendCommand("getCorridorSurfaces", { name: args.name })
        ),
    },
    get_feature_lines: {
      action: "get_feature_lines",
      inputSchema: z.object({ action: z.literal("get_feature_lines"), name: z.string() }),
      capabilities: ["query"],
      requiresActiveDrawing: true,
      safeForRetry: true,
      pluginMethods: ["getCorridorFeatureLines"],
      execute: async (args: any) =>
        await withApplicationConnection(async (c) =>
          await c.sendCommand("getCorridorFeatureLines", { name: args.name })
        ),
    },
    compute_volumes: {
      action: "compute_volumes",
      inputSchema: z.object({ action: z.literal("compute_volumes"), name: z.string() }),
      capabilities: ["query", "analyze"],
      requiresActiveDrawing: true,
      safeForRetry: true,
      pluginMethods: ["computeCorridorVolumes"],
      execute: async (args: any) =>
        await withApplicationConnection(async (c) =>
          await c.sendCommand("computeCorridorVolumes", { name: args.name })
        ),
    },
  },
  exposures: [
    {
      toolName: "civil3d_corridor",
      displayName: "Civil 3D Corridor",
      description:
        "Manage Civil 3D corridors (3D road models). Actions: list, get (by name), " +
        "rebuild, get_surfaces, get_feature_lines, compute_volumes.",
      inputShape: canonicalInputShape,
      supportedActions: [
        "list",
        "get",
        "rebuild",
        "get_surfaces",
        "get_feature_lines",
        "compute_volumes",
      ],
      resolveAction: (rawArgs) => ({
        action: String(rawArgs.action),
        args: rawArgs,
      }),
    },
  ],
};
