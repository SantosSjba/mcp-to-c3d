import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { registerAuditTool } from "./auditTool.js";
import { registerDiscoverTool } from "./discoverTool.js";
import { registerCommandTool } from "./commandTool.js";
import { registerExecuteTool } from "./executeTool.js";
import { registerHealthTool } from "./healthTool.js";
import { registerQueryTool } from "./queryTool.js";
import { registerSessionTool } from "./sessionTool.js";
import { registerSkillsTool } from "./skillsTool.js";
import { registerStatusTool } from "./statusTool.js";

/**
 * Register meta-tools for code execution architecture.
 */
export async function registerTools(server: McpServer) {
  registerHealthTool(server);
  registerStatusTool(server);
  registerDiscoverTool(server);
  registerAuditTool(server);
  registerExecuteTool(server);
  registerQueryTool(server);
  registerCommandTool(server);
  registerSessionTool(server);
  registerSkillsTool(server);
}
