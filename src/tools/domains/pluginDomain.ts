import { z } from "zod";
import { withApplicationConnection } from "../../utils/ConnectionManager.js";
import type { DomainToolDefinition } from "../domainRuntime.js";

export const PLUGIN_DOMAIN_DEFINITION: DomainToolDefinition = {
  domain: "plugin",
  actions: {
    health: {
      action: "health",
      inputSchema: z.object({}),
      capabilities: ["query"],
      requiresActiveDrawing: false,
      safeForRetry: true,
      pluginMethods: ["getCivil3DHealth"],
      execute: async () =>
        await withApplicationConnection(async (appClient) =>
          await appClient.sendCommand("getCivil3DHealth", {})
        ),
    },
  },
  exposures: [
    {
      toolName: "civil3d_health",
      displayName: "Civil 3D Health Check",
      description:
        "Checks if the Civil 3D MCP plugin is running and responsive. " +
        "Use this to verify connectivity before performing other operations.",
      inputShape: {},
      supportedActions: ["health"],
      resolveAction: () => ({ action: "health", args: {} }),
    },
  ],
};
