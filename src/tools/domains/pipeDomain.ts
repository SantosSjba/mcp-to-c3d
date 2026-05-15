import { z } from "zod";
import { withApplicationConnection } from "../../utils/ConnectionManager.js";
import type { DomainToolDefinition } from "../domainRuntime.js";

const PipeActionSchema = z.enum([
  "list_networks",
  "get_network",
  "get_pipe",
  "get_structure",
  "create_network",
  "add_pipe",
  "add_structure",
  "check_interference",
]);

const canonicalInputShape = {
  action: PipeActionSchema.describe("The pipe network operation to perform."),
  networkName: z.string().optional().describe("Pipe network name."),
  pipeName: z.string().optional().describe("Pipe name or handle."),
  structureName: z.string().optional().describe("Structure name or handle."),
  name: z.string().optional().describe("Name for new network."),
  partsList: z.string().optional().describe("Parts list to use."),
  style: z.string().optional().describe("Network style."),
  layer: z.string().optional().describe("Layer name."),
  startStructure: z.string().optional().describe("Start structure for new pipe."),
  endStructure: z.string().optional().describe("End structure for new pipe."),
  pipeSize: z.number().optional().describe("Pipe diameter/size."),
  insertionX: z.number().optional().describe("Insertion X for new structure."),
  insertionY: z.number().optional().describe("Insertion Y for new structure."),
  rimElevation: z.number().optional().describe("Rim elevation for structure."),
  sumpDepth: z.number().optional().describe("Sump depth for structure."),
};

export const PIPE_DOMAIN_DEFINITION: DomainToolDefinition = {
  domain: "pipe",
  actions: {
    list_networks: {
      action: "list_networks",
      inputSchema: z.object({ action: z.literal("list_networks") }),
      capabilities: ["query"],
      requiresActiveDrawing: true,
      safeForRetry: true,
      pluginMethods: ["listPipeNetworks"],
      execute: async () =>
        await withApplicationConnection(async (c) =>
          await c.sendCommand("listPipeNetworks", {})
        ),
    },
    get_network: {
      action: "get_network",
      inputSchema: z.object({
        action: z.literal("get_network"),
        networkName: z.string(),
      }),
      capabilities: ["query", "inspect"],
      requiresActiveDrawing: true,
      safeForRetry: true,
      pluginMethods: ["getPipeNetwork"],
      execute: async (args: any) =>
        await withApplicationConnection(async (c) =>
          await c.sendCommand("getPipeNetwork", { networkName: args.networkName })
        ),
    },
    get_pipe: {
      action: "get_pipe",
      inputSchema: z.object({
        action: z.literal("get_pipe"),
        networkName: z.string(),
        pipeName: z.string(),
      }),
      capabilities: ["query", "inspect"],
      requiresActiveDrawing: true,
      safeForRetry: true,
      pluginMethods: ["getPipe"],
      execute: async (args: any) =>
        await withApplicationConnection(async (c) =>
          await c.sendCommand("getPipe", {
            networkName: args.networkName,
            pipeName: args.pipeName,
          })
        ),
    },
    get_structure: {
      action: "get_structure",
      inputSchema: z.object({
        action: z.literal("get_structure"),
        networkName: z.string(),
        structureName: z.string(),
      }),
      capabilities: ["query", "inspect"],
      requiresActiveDrawing: true,
      safeForRetry: true,
      pluginMethods: ["getStructure"],
      execute: async (args: any) =>
        await withApplicationConnection(async (c) =>
          await c.sendCommand("getStructure", {
            networkName: args.networkName,
            structureName: args.structureName,
          })
        ),
    },
    create_network: {
      action: "create_network",
      inputSchema: z.object({
        action: z.literal("create_network"),
        name: z.string(),
        partsList: z.string().optional(),
        style: z.string().optional(),
        layer: z.string().optional(),
      }),
      capabilities: ["create"],
      requiresActiveDrawing: true,
      safeForRetry: false,
      pluginMethods: ["createPipeNetwork"],
      execute: async (args: any) =>
        await withApplicationConnection(async (c) =>
          await c.sendCommand("createPipeNetwork", {
            name: args.name,
            partsList: args.partsList,
            style: args.style,
            layer: args.layer,
          })
        ),
    },
    add_pipe: {
      action: "add_pipe",
      inputSchema: z.object({
        action: z.literal("add_pipe"),
        networkName: z.string(),
        startStructure: z.string(),
        endStructure: z.string(),
        pipeSize: z.number().optional(),
      }),
      capabilities: ["create", "edit"],
      requiresActiveDrawing: true,
      safeForRetry: false,
      pluginMethods: ["addPipeToNetwork"],
      execute: async (args: any) =>
        await withApplicationConnection(async (c) =>
          await c.sendCommand("addPipeToNetwork", {
            networkName: args.networkName,
            startStructure: args.startStructure,
            endStructure: args.endStructure,
            pipeSize: args.pipeSize,
          })
        ),
    },
    add_structure: {
      action: "add_structure",
      inputSchema: z.object({
        action: z.literal("add_structure"),
        networkName: z.string(),
        insertionX: z.number(),
        insertionY: z.number(),
        rimElevation: z.number().optional(),
        sumpDepth: z.number().optional(),
      }),
      capabilities: ["create", "edit"],
      requiresActiveDrawing: true,
      safeForRetry: false,
      pluginMethods: ["addStructureToNetwork"],
      execute: async (args: any) =>
        await withApplicationConnection(async (c) =>
          await c.sendCommand("addStructureToNetwork", {
            networkName: args.networkName,
            insertionX: args.insertionX,
            insertionY: args.insertionY,
            rimElevation: args.rimElevation,
            sumpDepth: args.sumpDepth,
          })
        ),
    },
    check_interference: {
      action: "check_interference",
      inputSchema: z.object({
        action: z.literal("check_interference"),
        networkName: z.string(),
      }),
      capabilities: ["query", "analyze"],
      requiresActiveDrawing: true,
      safeForRetry: true,
      pluginMethods: ["checkPipeNetworkInterference"],
      execute: async (args: any) =>
        await withApplicationConnection(async (c) =>
          await c.sendCommand("checkPipeNetworkInterference", {
            networkName: args.networkName,
          })
        ),
    },
  },
  exposures: [
    {
      toolName: "civil3d_pipe",
      displayName: "Civil 3D Pipe Network",
      description:
        "Manage gravity pipe networks. Actions: list_networks, get_network, " +
        "get_pipe, get_structure, create_network, add_pipe (between structures), " +
        "add_structure (at X,Y), check_interference.",
      inputShape: canonicalInputShape,
      supportedActions: [
        "list_networks",
        "get_network",
        "get_pipe",
        "get_structure",
        "create_network",
        "add_pipe",
        "add_structure",
        "check_interference",
      ],
      resolveAction: (rawArgs) => ({
        action: String(rawArgs.action),
        args: rawArgs,
      }),
    },
  ],
};
