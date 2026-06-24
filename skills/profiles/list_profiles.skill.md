---
name: list_profiles
category: profiles
description: List all profiles across all alignments with elevation ranges
requires_write: false
parameters:
  - name: alignmentName
    type: string
    required: false
    description: Filter by alignment name (optional)
---

## Code Template

```csharp
var profiles = new List<object>();
var filterAlignment = "ALIGNMENT_NAME_HERE"; // leave empty to list all

foreach (ObjectId alignId in CivilDoc.GetAlignmentIds())
{
    var alignment = Transaction.GetObject(alignId, OpenMode.ForRead) as Alignment;
    if (alignment == null) continue;

    if (!string.IsNullOrEmpty(filterAlignment) &&
        !alignment.Name.Equals(filterAlignment, StringComparison.OrdinalIgnoreCase))
        continue;

    foreach (ObjectId profId in alignment.GetProfileIds())
    {
        var profile = Transaction.GetObject(profId, OpenMode.ForRead) as Profile;
        if (profile == null) continue;

        profiles.Add(new {
            name = profile.Name,
            alignmentName = alignment.Name,
            type = profile.ProfileType.ToString(),
            handle = profile.Handle.ToString(),
            startStation = profile.StartingStation,
            endStation = profile.EndingStation,
            minElevation = profile.ElevationMin,
            maxElevation = profile.ElevationMax,
            style = profile.StyleName
        });
    }
}

return new { count = profiles.Count, profiles };
```

## Usage Notes
- Profile types include Surface, Layout, Superimposed, etc.
- Surface profiles are created from a surface; layout profiles are designed
- Use profiles with corridors for vertical design
