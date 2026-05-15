import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { z } from "zod";
import * as fs from "fs";
import * as path from "path";
import { createLogger } from "../utils/logger.js";
import { fileURLToPath } from "url";

const log = createLogger("SkillsTool");

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);
const SKILLS_DIR = path.resolve(__dirname, "..", "..", "skills");

interface SkillMetadata {
  name: string;
  category: string;
  description: string;
  requires_write: boolean;
  parameters: Array<{
    name: string;
    type: string;
    required: boolean;
    description?: string;
  }>;
}

interface SkillFile {
  metadata: SkillMetadata;
  content: string;
  filePath: string;
}

/**
 * Parse a .skill.md file into metadata + content.
 * Format: YAML frontmatter between --- delimiters, then markdown body.
 */
function parseSkillFile(filePath: string): SkillFile | null {
  try {
    const raw = fs.readFileSync(filePath, "utf-8");
    const frontmatterMatch = raw.match(/^---\n([\s\S]*?)\n---\n([\s\S]*)$/);

    if (!frontmatterMatch) {
      log.warn("Skill file missing frontmatter", { filePath });
      return null;
    }

    const yamlBlock = frontmatterMatch[1];
    const content = frontmatterMatch[2].trim();

    // Simple YAML parsing (no dependency needed for our format)
    const metadata: SkillMetadata = {
      name: extractYamlValue(yamlBlock, "name") ?? path.basename(filePath, ".skill.md"),
      category: extractYamlValue(yamlBlock, "category") ?? "general",
      description: extractYamlValue(yamlBlock, "description") ?? "",
      requires_write: extractYamlValue(yamlBlock, "requires_write") === "true",
      parameters: extractYamlParameters(yamlBlock),
    };

    return { metadata, content, filePath };
  } catch (error) {
    log.error("Error parsing skill file", { filePath, error: String(error) });
    return null;
  }
}

function extractYamlValue(yaml: string, key: string): string | undefined {
  const match = yaml.match(new RegExp(`^${key}:\\s*(.+)$`, "m"));
  return match ? match[1].trim() : undefined;
}

function extractYamlParameters(yaml: string): SkillMetadata["parameters"] {
  const params: SkillMetadata["parameters"] = [];
  const paramSection = yaml.match(/parameters:\n((?:\s+-[\s\S]*?)*)(?:\n\w|$)/);
  if (!paramSection) return params;

  const paramBlocks = paramSection[1].split(/\n\s+-\s+/).filter(Boolean);
  for (const block of paramBlocks) {
    const lines = block.includes("- ") ? block.replace(/^\s*-\s*/, "") : block;
    const name = extractYamlValue(lines, "name");
    const type = extractYamlValue(lines, "type") ?? "string";
    const required = extractYamlValue(lines, "required") === "true";
    const description = extractYamlValue(lines, "description");
    if (name) {
      params.push({ name, type, required, description });
    }
  }

  return params;
}

/**
 * Recursively find all .skill.md files in the skills directory.
 */
function findSkillFiles(dir: string): string[] {
  if (!fs.existsSync(dir)) return [];

  const results: string[] = [];
  const entries = fs.readdirSync(dir, { withFileTypes: true });

  for (const entry of entries) {
    const fullPath = path.join(dir, entry.name);
    if (entry.isDirectory()) {
      results.push(...findSkillFiles(fullPath));
    } else if (entry.name.endsWith(".skill.md")) {
      results.push(fullPath);
    }
  }

  return results;
}

/**
 * Get all available skills, optionally filtered by category or search query.
 */
function getSkills(category?: string, query?: string): SkillFile[] {
  const files = findSkillFiles(SKILLS_DIR);
  let skills = files.map(parseSkillFile).filter((s): s is SkillFile => s !== null);

  if (category) {
    skills = skills.filter(
      (s) => s.metadata.category.toLowerCase() === category.toLowerCase()
    );
  }

  if (query) {
    const q = query.toLowerCase();
    skills = skills.filter(
      (s) =>
        s.metadata.name.toLowerCase().includes(q) ||
        s.metadata.description.toLowerCase().includes(q) ||
        s.metadata.category.toLowerCase().includes(q)
    );
  }

  return skills;
}

export function registerSkillsTool(server: McpServer) {
  server.tool(
    "civil3d_skills",
    "Browse and read Civil 3D code skills (documented C# code templates). " +
      "Use 'list' to see available skills, 'search' to find by keyword, " +
      "'get' to read the full skill with code template. " +
      "Skills are pre-built C# patterns you can adapt and execute via civil3d_execute or civil3d_query.",
    {
      action: z
        .enum(["list", "search", "get"])
        .describe("list = show all skills, search = find by keyword, get = read full skill"),
      category: z.string().optional().describe("Filter by category (surfaces, alignments, points, etc.)"),
      query: z.string().optional().describe("Search query for 'search' action"),
      skillName: z.string().optional().describe("Skill name for 'get' action"),
    },
    async (args) => {
      try {
        switch (args.action) {
          case "list": {
            const skills = getSkills(args.category);
            const summary = skills.map((s) => ({
              name: s.metadata.name,
              category: s.metadata.category,
              description: s.metadata.description,
              requires_write: s.metadata.requires_write,
              parameters: s.metadata.parameters.map((p) => p.name),
            }));

            return {
              content: [
                {
                  type: "text" as const,
                  text: JSON.stringify({ count: summary.length, skills: summary }, null, 2),
                },
              ],
            };
          }

          case "search": {
            if (!args.query) {
              return {
                content: [{ type: "text" as const, text: "Parameter 'query' is required for search." }],
                isError: true,
              };
            }

            const skills = getSkills(undefined, args.query);
            const results = skills.map((s) => ({
              name: s.metadata.name,
              category: s.metadata.category,
              description: s.metadata.description,
            }));

            return {
              content: [
                {
                  type: "text" as const,
                  text: JSON.stringify({ count: results.length, results }, null, 2),
                },
              ],
            };
          }

          case "get": {
            if (!args.skillName) {
              return {
                content: [{ type: "text" as const, text: "Parameter 'skillName' is required for get." }],
                isError: true,
              };
            }

            const allSkills = getSkills();
            const skill = allSkills.find(
              (s) => s.metadata.name.toLowerCase() === args.skillName!.toLowerCase()
            );

            if (!skill) {
              const available = allSkills.map((s) => s.metadata.name).join(", ");
              return {
                content: [
                  {
                    type: "text" as const,
                    text: `Skill '${args.skillName}' not found. Available: ${available}`,
                  },
                ],
                isError: true,
              };
            }

            return {
              content: [
                {
                  type: "text" as const,
                  text: JSON.stringify(
                    {
                      ...skill.metadata,
                      content: skill.content,
                    },
                    null,
                    2
                  ),
                },
              ],
            };
          }
        }
      } catch (error) {
        const message = error instanceof Error ? error.message : String(error);
        log.error("Skills operation failed", { error: message });
        return {
          content: [{ type: "text" as const, text: `Skills error: ${message}` }],
          isError: true,
        };
      }
    }
  );
}
