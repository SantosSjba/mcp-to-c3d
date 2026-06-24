---
name: corridor_quantities
category: quantity
description: Get material quantities for a corridor by name
requires_write: false
parameters:
  - name: corridorName
    type: string
    required: true
    description: Name of the corridor
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

var materials = new List<object>();

// Query material quantities per region
foreach (CorridorRegion region in corridor.CorridorRegions)
{
    try
    {
        var qty = region.GetRegionQuantities();
        foreach (var item in qty)
        {
            materials.Add(new {
                regionName = region.Name,
                materialName = item.Key,
                quantity = item.Value
            });
        }
    }
    catch
    {
        materials.Add(new {
            regionName = region.Name,
            note = "Quantities not computed — rebuild corridor first"
        });
    }
}

return new {
    corridorName = corridor.Name,
    regionCount = corridor.CorridorRegions.Count,
    materials,
    hint = materials.Count == 0
        ? "Run civil3d_command({ command: 'AeccCorridorRebuild' }) then retry"
        : null
};
```

## Usage Notes
- Corridor must be rebuilt before quantities are available
- Material names come from the assembly definition
- Use `civil3d_command` with `AeccCorridorRebuild` if quantities are empty
