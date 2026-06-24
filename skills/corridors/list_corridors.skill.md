---
name: list_corridors
category: corridors
description: List all corridors with baselines, regions, and style information
requires_write: false
parameters: []
---

## Code Template

```csharp
var corridors = new List<object>();

foreach (ObjectId id in CivilDoc.GetCorridorIds())
{
    var corridor = Transaction.GetObject(id, OpenMode.ForRead) as Corridor;
    if (corridor == null) continue;

    var baselines = new List<object>();
    foreach (Baseline baseline in corridor.Baselines)
    {
        baselines.Add(new {
            name = baseline.Name,
            alignmentName = baseline.AlignmentName,
            profileName = baseline.ProfileName,
            startStation = baseline.StartStation,
            endStation = baseline.EndStation,
            regionCount = baseline.BaselineRegions.Count
        });
    }

    corridors.Add(new {
        name = corridor.Name,
        handle = corridor.Handle.ToString(),
        layer = corridor.Layer,
        style = corridor.StyleName,
        baselineCount = corridor.Baselines.Count,
        regionCount = corridor.CorridorRegions.Count,
        baselines
    });
}

return new { count = corridors.Count, corridors };
```

## Usage Notes
- Corridors require an alignment, profile, and assembly to build
- Use `corridor_info` for detailed quantities on a specific corridor
- To create corridors, use `create_corridor_workflow` skill or `civil3d_command`
