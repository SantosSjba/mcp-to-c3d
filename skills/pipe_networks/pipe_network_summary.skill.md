---
name: pipe_network_summary
category: pipe_networks
description: Get detailed pipes and structures for a specific pipe network
requires_write: false
parameters:
  - name: networkName
    type: string
    required: true
    description: Name of the pipe network
---

## Code Template

```csharp
Network network = null;
foreach (ObjectId id in CivilDoc.GetPipeNetworkIds())
{
    var n = Transaction.GetObject(id, OpenMode.ForRead) as Network;
    if (n != null && n.Name.Equals("NETWORK_NAME_HERE", StringComparison.OrdinalIgnoreCase))
    { network = n; break; }
}
if (network == null) return new { error = "Network not found" };

var pipes = new List<object>();
foreach (ObjectId pipeId in network.GetPipeIds())
{
    var pipe = Transaction.GetObject(pipeId, OpenMode.ForRead) as Pipe;
    if (pipe == null) continue;
    pipes.Add(new {
        name = pipe.Name,
        partFamily = pipe.PartFamilyName,
        partSize = pipe.PartSizeName,
        startStructure = pipe.StartStructureName,
        endStructure = pipe.EndStructureName,
        length = pipe.Length2DCenterToCenter,
        slope = pipe.Slope,
        handle = pipe.Handle.ToString()
    });
}

var structures = new List<object>();
foreach (ObjectId structId in network.GetStructureIds())
{
    var structure = Transaction.GetObject(structId, OpenMode.ForRead) as Structure;
    if (structure == null) continue;
    structures.Add(new {
        name = structure.Name,
        partFamily = structure.PartFamilyName,
        partSize = structure.PartSizeName,
        rimElevation = structure.RimElevation,
        sumpElevation = structure.SumpElevation,
        handle = structure.Handle.ToString()
    });
}

return new {
    networkName = network.Name,
    pipeCount = pipes.Count,
    structureCount = structures.Count,
    pipes,
    structures
};
```

## Usage Notes
- Replace NETWORK_NAME_HERE with the target network name
- Pipe properties include 2D center-to-center length and slope
- Structure rim/sump elevations are critical for hydraulic design
