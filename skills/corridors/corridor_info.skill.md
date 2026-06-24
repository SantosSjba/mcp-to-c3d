---
name: corridor_info
category: corridors
description: Get detailed information about a specific corridor by name
requires_write: false
parameters:
  - name: corridorName
    type: string
    required: true
    description: Name of the corridor to inspect
---

## Code Template

```csharp
Corridor corridor = null;
foreach (ObjectId id in CivilDoc.GetCorridorIds())
{
    var c = Transaction.GetObject(id, OpenMode.ForRead) as Corridor;
    if (c != null && c.Name.Equals("CORRIDOR_NAME_HERE", StringComparison.OrdinalIgnoreCase))
    { corridor = c; break; }
}
if (corridor == null) return new { error = "Corridor not found" };

var regions = new List<object>();
foreach (CorridorRegion region in corridor.CorridorRegions)
{
    regions.Add(new {
        name = region.Name,
        assemblyName = region.AssemblyName,
        startStation = region.StartStation,
        endStation = region.EndStation,
        baselineName = region.BaselineName
    });
}

return new {
    name = corridor.Name,
    handle = corridor.Handle.ToString(),
    style = corridor.StyleName,
    baselineCount = corridor.Baselines.Count,
    regionCount = corridor.CorridorRegions.Count,
    regions
};
```

## Usage Notes
- Replace CORRIDOR_NAME_HERE with the target corridor name
- For material quantities, use `corridor_quantities` skill
- Rebuild corridor after changes with `civil3d_command({ command: "AeccCorridorRebuild" })`
