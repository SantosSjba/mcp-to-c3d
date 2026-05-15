import { z } from "zod";
import { withApplicationConnection } from "../../utils/ConnectionManager.js";
import type { DomainToolDefinition } from "../domainRuntime.js";

const DrawingActionSchema = z.enum([
  "info",
  "settings",
  "save",
  "new",
  "undo",
  "redo",
  "list_object_types",
  "get_selected",
]);

const canonicalInputShape = {
  action: DrawingActionSchema.describe("The drawing operation to perform."),
  limit: z.number().optional().describe("Max objects to return (for get_selected)."),
};

export const DRAWING_DOMAIN_DEFINITION: DomainToolDefinition = {
  domain: "drawing",
  actions: {
    info: {
      action: "info",
      inputSchema: z.object({ action: z.literal("info") }),
      capabilities: ["query"],
      requiresActiveDrawing: true,
      safeForRetry: true,
      pluginMethods: ["getDrawingInfo"],
      execute: async () =>
        await withApplicationConnection(async (c) =>
          await c.sendCommand("getDrawingInfo", {})
        ),
    },
    settings: {
      action: "settings",
      inputSchema: z.object({ action: z.literal("settings") }),
      capabilities: ["query"],
      requiresActiveDrawing: true,
      safeForRetry: true,
      pluginMethods: ["getDrawingSettings"],
      execute: async () =>
        await withApplicationConnection(async (c) =>
          await c.sendCommand("getDrawingSettings", {})
        ),
    },
    save: {
      action: "save",
      inputSchema: z.object({ action: z.literal("save") }),
      capabilities: ["manage"],
      requiresActiveDrawing: true,
      safeForRetry: false,
      pluginMethods: ["saveDrawing"],
      execute: async () =>
        await withApplicationConnection(async (c) =>
          await c.sendCommand("saveDrawing", {})
        ),
    },
    new: {
      action: "new",
      inputSchema: z.object({
        action: z.literal("new"),
        templatePath: z.string().optional(),
      }),
      capabilities: ["create"],
      requiresActiveDrawing: false,
      safeForRetry: false,
      pluginMethods: ["newDrawing"],
      execute: async (args: any) =>
        await withApplicationConnection(async (c) =>
          await c.sendCommand("newDrawing", { templatePath: args.templatePath })
        ),
    },
    undo: {
      action: "undo",
      inputSchema: z.object({ action: z.literal("undo") }),
      capabilities: ["manage"],
      requiresActiveDrawing: true,
      safeForRetry: false,
      pluginMethods: ["undoDrawing"],
      execute: async () =>
        await withApplicationConnection(async (c) =>
          await c.sendCommand("undoDrawing", {})
        ),
    },
    redo: {
      action: "redo",
      inputSchema: z.object({ action: z.literal("redo") }),
      capabilities: ["manage"],
      requiresActiveDrawing: true,
      safeForRetry: false,
      pluginMethods: ["redoDrawing"],
      execute: async () =>
        await withApplicationConnection(async (c) =>
          await c.sendCommand("redoDrawing", {})
        ),
    },
    list_object_types: {
      action: "list_object_types",
      inputSchema: z.object({ action: z.literal("list_object_types") }),
      capabilities: ["query"],
      requiresActiveDrawing: true,
      safeForRetry: true,
      pluginMethods: ["listCivilObjectTypes"],
      execute: async () =>
        await withApplicationConnection(async (c) =>
          await c.sendCommand("listCivilObjectTypes", {})
        ),
    },
    get_selected: {
      action: "get_selected",
      inputSchema: z.object({
        action: z.literal("get_selected"),
        limit: z.number().optional(),
      }),
      capabilities: ["query", "inspect"],
      requiresActiveDrawing: true,
      safeForRetry: true,
      pluginMethods: ["getSelectedCivilObjectsInfo"],
      execute: async (args: any) =>
        await withApplicationConnection(async (c) =>
          await c.sendCommand("getSelectedCivilObjectsInfo", {
            limit: args.limit ?? 100,
          })
        ),
    },
  },
  exposures: [
    {
      toolName: "civil3d_drawing",
      displayName: "Civil 3D Drawing",
      description:
        "Manage the active Civil 3D drawing. Actions: info (get drawing info), " +
        "settings (get drawing settings), save, new, undo, redo, " +
        "list_object_types (list available Civil 3D object types), " +
        "get_selected (get info about selected objects).",
      inputShape: canonicalInputShape,
      supportedActions: [
        "info",
        "settings",
        "save",
        "new",
        "undo",
        "redo",
        "list_object_types",
        "get_selected",
      ],
      resolveAction: (rawArgs) => ({
        action: String(rawArgs.action),
        args: rawArgs,
      }),
    },
  ],
};
