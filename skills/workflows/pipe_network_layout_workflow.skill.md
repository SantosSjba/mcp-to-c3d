---
name: pipe_network_layout_workflow
category: workflows
description: Workflow for analyzing and laying out pipe networks
requires_write: false
parameters: []
---

## Workflow Steps

### Step 1 — Discover pipe networks
```
civil3d_discover({ categories: ["pipeNetworks", "surfaces"] })
```

### Step 2 — Get network details
Use `pipe_network_summary` skill with the network name.

### Step 3 — Verify reference surface
Pipe networks need a reference surface for rim/sump calculations:
```csharp
// In civil3d_query:
var network = /* find by name */;
return new {
    name = network.Name,
    referenceSurface = network.ReferenceSurfaceName,
    partsList = network.PartsListName,
    pipeCount = network.GetPipeIds().Count,
    structureCount = network.GetStructureIds().Count
};
```

### Step 4 — Create new network (write)
Use `civil3d_session` for multi-step creation, or:
```
civil3d_command({ command: "CreatePipeNetwork", waitForCompletion: true })
```

### Step 5 — Export network data
Use `export_cogo_points_csv` for related points, or LandXML for full export.

## Key API Patterns

```csharp
// List all pipes in a network
foreach (ObjectId pipeId in network.GetPipeIds())
{
    var pipe = Transaction.GetObject(pipeId, OpenMode.ForRead) as Pipe;
    // pipe.StartStructureName, pipe.EndStructureName, pipe.Slope, pipe.Length2DCenterToCenter
}

// List all structures
foreach (ObjectId structId in network.GetStructureIds())
{
    var structure = Transaction.GetObject(structId, OpenMode.ForRead) as Structure;
    // structure.RimElevation, structure.SumpElevation
}
```

## Usage Notes
- Always check parts list compatibility before adding pipes
- Structures must exist before connecting pipes between them
- Use `civil3d_discover` first to avoid duplicate networks
