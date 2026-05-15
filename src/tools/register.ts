import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { registerDomainTools } from "./domainRuntime.js";
import { PLUGIN_DOMAIN_DEFINITION } from "./domains/pluginDomain.js";
import { DRAWING_DOMAIN_DEFINITION } from "./domains/drawingDomain.js";
import { SURFACE_DOMAIN_DEFINITION } from "./domains/surfaceDomain.js";
import { ALIGNMENT_DOMAIN_DEFINITION } from "./domains/alignmentDomain.js";
import { PROFILE_DOMAIN_DEFINITION } from "./domains/profileDomain.js";
import { CORRIDOR_DOMAIN_DEFINITION } from "./domains/corridorDomain.js";
import { PIPE_DOMAIN_DEFINITION } from "./domains/pipeDomain.js";
import { POINT_DOMAIN_DEFINITION } from "./domains/pointDomain.js";
import { GEOMETRY_DOMAIN_DEFINITION } from "./domains/geometryDomain.js";

/**
 * All domain definitions — order matters for tool discovery.
 * Plugin/health first, then read-heavy domains, then write-heavy.
 */
const DOMAIN_DEFINITIONS = [
  PLUGIN_DOMAIN_DEFINITION,
  DRAWING_DOMAIN_DEFINITION,
  SURFACE_DOMAIN_DEFINITION,
  ALIGNMENT_DOMAIN_DEFINITION,
  PROFILE_DOMAIN_DEFINITION,
  CORRIDOR_DOMAIN_DEFINITION,
  PIPE_DOMAIN_DEFINITION,
  POINT_DOMAIN_DEFINITION,
  GEOMETRY_DOMAIN_DEFINITION,
];

export async function registerTools(server: McpServer) {
  for (const definition of DOMAIN_DEFINITIONS) {
    registerDomainTools(server, definition);
  }
}
