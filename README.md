# Civil 3D MCP Server

An MCP (Model Context Protocol) server that enables AI assistants (Claude, Cline, etc.) to interact with Autodesk Civil 3D through natural language.

## Architecture

```
┌─────────────────┐     stdio      ┌──────────────────┐     TCP/JSON-RPC    ┌──────────────────┐
│   AI Assistant   │ ◄────────────► │  MCP Server (TS) │ ◄──────────────────► │  Civil 3D Plugin │
│ (Claude, Cline)  │               │   Node.js         │     port 8080       │   (.NET 8.0 C#)  │
└─────────────────┘               └──────────────────┘                      └──────────────────┘
                                                                                     │
                                                                              Civil 3D API
                                                                            (Surfaces, Alignments,
                                                                             Points, Corridors...)
```

The system has two components:
1. **MCP Server** (TypeScript/Node.js) — Communicates with AI assistants via MCP protocol
2. **Civil 3D Plugin** (C# .NET 8.0) — Runs inside Civil 3D and executes API commands

## Available Tools

| Tool | Actions | Description |
|------|---------|-------------|
| `civil3d_health` | health | Check plugin connectivity |
| `civil3d_drawing` | info, settings, save, undo, redo, list_object_types, get_selected | Drawing operations |
| `civil3d_surface` | list, get, get_elevation, get_statistics, create, delete, add_points, add_breakline, add_boundary, extract_contours, compute_volume | Surface management |
| `civil3d_alignment` | list, get, create, delete, station_to_point, point_to_station | Alignment operations |
| `civil3d_profile` | list, get, get_elevation, create_from_surface, create_layout, delete | Profile management |
| `civil3d_corridor` | list, get, rebuild, get_surfaces, get_feature_lines, compute_volumes | Corridor operations |
| `civil3d_pipe` | list_networks, get_network, get_pipe, get_structure, create_network, add_pipe, add_structure, check_interference | Pipe networks |
| `civil3d_point` | list, get, create, delete, list_groups, import | COGO points |
| `civil3d_geometry` | create_line, create_polyline, create_3d_polyline, create_text, create_mtext | Basic AutoCAD geometry |

## Setup

### 1. Build the MCP Server

```bash
npm install
npm run build
```

### 2. Build the Civil 3D Plugin

1. Copy the required DLLs from your Civil 3D installation to `C_References/` (see [C_References/README.md](C_References/README.md))
2. Build the plugin:

```bash
cd plugin/Civil3dMcpPlugin
dotnet build
```

### 3. Load the Plugin in Civil 3D

1. Open Civil 3D 2025+
2. Type `NETLOAD` in the command line
3. Browse to `plugin/Civil3dMcpPlugin/bin/Debug/net8.0-windows/Civil3dMcpPlugin.dll`
4. The plugin starts automatically. Use `C3DMCPSTATUS` to verify.

### 4. Configure Your AI Assistant

**Claude Desktop** — Add to `claude_desktop_config.json`:

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
| `CIVIL3D_HOST` | `localhost` | Civil 3D plugin host |
| `CIVIL3D_PORT` | `8080` | Civil 3D plugin port |
| `CIVIL3D_CONNECT_TIMEOUT` | `5000` | Connection timeout (ms) |
| `CIVIL3D_COMMAND_TIMEOUT` | `120000` | Command execution timeout (ms) |
| `LOG_LEVEL` | `info` | Log level (debug, info, warn, error) |

## Plugin Commands

| Command | Description |
|---------|-------------|
| `C3DMCPSTART` | Start the TCP listener |
| `C3DMCPSTOP` | Stop the TCP listener |
| `C3DMCPSTATUS` | Check listener status |

## License

ISC
