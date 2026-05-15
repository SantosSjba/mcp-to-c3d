import { z } from "zod";
import { withApplicationConnection } from "../../utils/ConnectionManager.js";
import type { DomainToolDefinition } from "../domainRuntime.js";

const ProfileActionSchema = z.enum([
  "list",
  "get",
  "get_elevation",
  "create_from_surface",
  "create_layout",
  "delete",
]);

const canonicalInputShape = {
  action: ProfileActionSchema.describe("The profile operation to perform."),
  alignmentName: z.string().optional().describe("Parent alignment name."),
  name: z.string().optional().describe("Profile name."),
  station: z.number().optional().describe("Station for elevation query."),
  surfaceName: z.string().optional().describe("Surface to sample."),
  style: z.string().optional().describe("Profile style."),
  layer: z.string().optional().describe("Layer name."),
};

export const PROFILE_DOMAIN_DEFINITION: DomainToolDefinition = {
  domain: "profile",
  actions: {
    list: {
      action: "list",
      inputSchema: z.object({
        action: z.literal("list"),
        alignmentName: z.string(),
      }),
      capabilities: ["query"],
      requiresActiveDrawing: true,
      safeForRetry: true,
      pluginMethods: ["listProfiles"],
      execute: async (args: any) =>
        await withApplicationConnection(async (c) =>
          await c.sendCommand("listProfiles", { alignmentName: args.alignmentName })
        ),
    },
    get: {
      action: "get",
      inputSchema: z.object({
        action: z.literal("get"),
        alignmentName: z.string(),
        name: z.string(),
      }),
      capabilities: ["query", "inspect"],
      requiresActiveDrawing: true,
      safeForRetry: true,
      pluginMethods: ["getProfile"],
      execute: async (args: any) =>
        await withApplicationConnection(async (c) =>
          await c.sendCommand("getProfile", {
            alignmentName: args.alignmentName,
            name: args.name,
          })
        ),
    },
    get_elevation: {
      action: "get_elevation",
      inputSchema: z.object({
        action: z.literal("get_elevation"),
        alignmentName: z.string(),
        name: z.string(),
        station: z.number(),
      }),
      capabilities: ["query", "analyze"],
      requiresActiveDrawing: true,
      safeForRetry: true,
      pluginMethods: ["getProfileElevation"],
      execute: async (args: any) =>
        await withApplicationConnection(async (c) =>
          await c.sendCommand("getProfileElevation", {
            alignmentName: args.alignmentName,
            name: args.name,
            station: args.station,
          })
        ),
    },
    create_from_surface: {
      action: "create_from_surface",
      inputSchema: z.object({
        action: z.literal("create_from_surface"),
        alignmentName: z.string(),
        surfaceName: z.string(),
        name: z.string().optional(),
        style: z.string().optional(),
        layer: z.string().optional(),
      }),
      capabilities: ["create"],
      requiresActiveDrawing: true,
      safeForRetry: false,
      pluginMethods: ["createProfileFromSurface"],
      execute: async (args: any) =>
        await withApplicationConnection(async (c) =>
          await c.sendCommand("createProfileFromSurface", {
            alignmentName: args.alignmentName,
            surfaceName: args.surfaceName,
            name: args.name,
            style: args.style,
            layer: args.layer,
          })
        ),
    },
    create_layout: {
      action: "create_layout",
      inputSchema: z.object({
        action: z.literal("create_layout"),
        alignmentName: z.string(),
        name: z.string(),
        style: z.string().optional(),
        layer: z.string().optional(),
      }),
      capabilities: ["create"],
      requiresActiveDrawing: true,
      safeForRetry: false,
      pluginMethods: ["createLayoutProfile"],
      execute: async (args: any) =>
        await withApplicationConnection(async (c) =>
          await c.sendCommand("createLayoutProfile", {
            alignmentName: args.alignmentName,
            name: args.name,
            style: args.style,
            layer: args.layer,
          })
        ),
    },
    delete: {
      action: "delete",
      inputSchema: z.object({
        action: z.literal("delete"),
        alignmentName: z.string(),
        name: z.string(),
      }),
      capabilities: ["delete"],
      requiresActiveDrawing: true,
      safeForRetry: false,
      pluginMethods: ["deleteProfile"],
      execute: async (args: any) =>
        await withApplicationConnection(async (c) =>
          await c.sendCommand("deleteProfile", {
            alignmentName: args.alignmentName,
            name: args.name,
          })
        ),
    },
  },
  exposures: [
    {
      toolName: "civil3d_profile",
      displayName: "Civil 3D Profile",
      description:
        "Manage Civil 3D profiles (vertical geometry). Actions: list (by alignment), " +
        "get (by alignment + name), get_elevation (at station), " +
        "create_from_surface, create_layout, delete.",
      inputShape: canonicalInputShape,
      supportedActions: [
        "list",
        "get",
        "get_elevation",
        "create_from_surface",
        "create_layout",
        "delete",
      ],
      resolveAction: (rawArgs) => ({
        action: String(rawArgs.action),
        args: rawArgs,
      }),
    },
  ],
};
