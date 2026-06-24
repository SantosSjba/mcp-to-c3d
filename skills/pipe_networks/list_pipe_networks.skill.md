---
name: list_pipe_networks
category: pipe_networks
description: List all pipe networks with pipe and structure counts
requires_write: false
parameters: []
---

## Code Template

```csharp
var networks = new List<object>();

foreach (ObjectId id in CivilDoc.GetPipeNetworkIds())
{
    var network = Transaction.GetObject(id, OpenMode.ForRead) as Network;
    if (network == null) continue;

    networks.Add(new {
        name = network.Name,
        handle = network.Handle.ToString(),
        layer = network.Layer,
        pipeCount = network.GetPipeIds().Count,
        structureCount = network.GetStructureIds().Count,
        referenceSurface = network.ReferenceSurfaceName,
        partsList = network.PartsListName
    });
}

return new { count = networks.Count, networks };
```

## Usage Notes
- Networks contain pipes (lines) and structures (manholes, inlets, etc.)
- Use `pipe_network_summary` for detailed pipe/structure listing
- Parts list defines available pipe and structure types
