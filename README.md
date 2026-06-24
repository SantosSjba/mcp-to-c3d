# Civil 3D MCP Server — Code Execution Architecture

An MCP server that enables AI assistants to **write and execute C# code** directly inside Autodesk Civil 3D. Instead of fixed tools, the AI generates code that runs with full API access.

## Architecture

```
┌─────────────────┐     stdio      ┌──────────────────┐     TCP/JSON-RPC    ┌──────────────────┐
│   AI Assistant   │ ◄────────────► │  MCP Server (TS) │ ◄──────────────────► │  Civil 3D Plugin │
│ (Claude, Cline)  │               │   3 meta-tools    │     port 8080       │  Roslyn Engine   │
└─────────────────┘               └──────────────────┘                      └──────────────────┘
                                         │                                         │
                                    Skills Library                           C# Code Execution
                                   (.skill.md files)                      (full Civil 3D API)
```

## 9 Meta-Tools

| Tool | Purpose | Safety |
|------|---------|--------|
| `civil3d_health` | Check connection, Civil 3D version, active drawing | ✅ Read-only |
| `civil3d_status` | Real-time operation progress and queue state | ✅ Read-only |
| `civil3d_discover` | Inventory drawing objects without writing C# | ✅ Read-only |
| `civil3d_audit` | Read audit log of past operations | ✅ Read-only |
| `civil3d_query` | Execute C# code **read-only** | ✅ No side effects |
| `civil3d_execute` | Execute C# code with **write** access | ⚠️ Modifies drawing |
| `civil3d_command` | Execute native Civil 3D command strings | ⚠️ May modify drawing |
| `civil3d_session` | Multi-step transactions (begin/execute/commit/abort) | ⚠️ May modify drawing |
| `civil3d_skills` | Browse/search/read code skill templates | ✅ Metadata only |

### How It Works

1. **AI reads a skill** → Gets a documented C# code template
2. **AI adapts the code** → Fills in parameters, combines patterns
3. **AI sends code** → Via `civil3d_execute` or `civil3d_query`
4. **Roslyn compiles + runs** → Inside Civil 3D with full API access
5. **Results return as JSON** → Back to the AI

### Example Interaction

```
User: "What surfaces are in my drawing?"

AI: Uses civil3d_query with:
  var surfaces = new List<object>();
  foreach (ObjectId id in CivilDoc.GetSurfaceIds()) {
    var s = Transaction.GetObject(id, OpenMode.ForRead) as TinSurface;
    surfaces.Add(new { s.Name, s.Layer });
  }
  return surfaces;

Result: [{ "Name": "EG", "Layer": "C-TOPO-EG" }, ...]
```

## Skills Library

Skills are documented C# code templates in `skills/`:

```
skills/
├── surfaces/           # Surface operations
├── alignments/         # Alignment + station/offset
├── points/             # COGO points
├── geometry/           # Lines, polylines, text
├── corridors/          # Corridor listing and info
├── pipe_networks/      # Pipe network listing and details
├── parcels/            # Sites and parcels
├── profiles/           # Profile listing and elevation queries
├── sections/           # Sample lines and cross-sections
├── labels/             # Label style inventories
├── styles/             # Object style inventories
├── export/             # CSV, LandXML export patterns
├── quantity/           # Volumes and corridor quantities
├── drawing/            # Drawing info
└── workflows/          # Multi-step civil engineering workflows
```

### Script Globals

Code executed via `civil3d_execute` or `civil3d_query` has access to:

| Global | Type | Description |
|--------|------|-------------|
| `Document` / `Doc` | `Document` | Active AutoCAD document |
| `CivilDoc` / `Civil` | `CivilDocument` | Active Civil 3D document |
| `Database` | `Database` | Document database |
| `Transaction` / `Tr` | `Transaction` | Active transaction |
| `Editor` | `Editor` | Document editor |

**Helper methods:** `GetSurfaceByName`, `GetAlignmentByName`, `GetProfileByName`, `GetCogoPointByNumber`, `GetObjectIdByHandle`, `ListSurfaces`, `ListAlignments`, `ListCogoPoints`, `ToRef`, `ToPoint`

All Civil 3D namespaces are auto-imported. Return values are auto-serialized (ObjectId → handle, Point3d → xyz).

## Setup

### Quick setup (recommended)
```powershell
npm run setup
```
This interactive script will:
1. Detect installed Civil 3D versions
2. Configure plugin DLL references automatically
3. Build the MCP server and plugin
4. Print MCP config for Cursor

### Manual setup

#### 1. Build MCP Server
```bash
npm install && npm run build
```

#### 2. Build Plugin
```bash
# References are in plugin/Civil3dMcpPlugin/Civil3dMcpPlugin.References.props
# Regenerate with: npm run setup
cd plugin/Civil3dMcpPlugin
dotnet build
```

### 3. Load in Civil 3D
```
NETLOAD → select Civil3dMcpPlugin.dll
C3DMCPSTATUS → verify running
```

### 4. Configure AI
```json
{
  "mcpServers": {
    "civil3d": {
      "command": "node",
      "args": ["/path/to/civil3d-mcp/build/index.js"]
    }
  }
}
```

## Environment Variables

| Variable | Default | Description |
|----------|---------|-------------|
| `CIVIL3D_HOST` | `localhost` | Plugin host |
| `CIVIL3D_PORT` | `8080` | Plugin port (set on **both** MCP server and Civil 3D process) |
| `CIVIL3D_DEFAULT_TIMEOUT_MS` | `120000` | Default operation timeout (ms) |
| `CIVIL3D_EXECUTE_TIMEOUT_MS` | `120000` | C# script execution timeout |
| `CIVIL3D_COMMAND_TIMEOUT_MS` | `300000` | Native command timeout |
| `CIVIL3D_DISCOVER_TIMEOUT_MS` | `60000` | Discover inventory timeout |
| `CIVIL3D_AUDIT_LOG` | `%LOCALAPPDATA%\Civil3dMcp\audit.jsonl` | Audit log file path |
| `LOG_LEVEL` | `info` | Log level |

## Development Roadmap

See [ROADMAP.md](./ROADMAP.md) for the phased plan to reach professional full access.

## Security

The Roslyn sandbox blocks:
- Process execution (`Process.Start`)
- File deletion (`File.Delete`)
- Network requests (`HttpClient`, `Sockets`)
- Registry access
- Dynamic assembly loading

All Civil 3D API operations are allowed.

## License

  MIT
