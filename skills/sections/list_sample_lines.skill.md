---
name: list_sample_lines
category: sections
description: List sample line groups and sample lines for cross-section analysis
requires_write: false
parameters: []
---

## Code Template

```csharp
var sampleLines = new List<object>();

foreach (ObjectId groupId in CivilDoc.GetSampleLineGroupIds())
{
    var group = Transaction.GetObject(groupId, OpenMode.ForRead) as SampleLineGroup;
    if (group == null) continue;

    foreach (ObjectId lineId in group.GetSampleLineIds())
    {
        var line = Transaction.GetObject(lineId, OpenMode.ForRead) as SampleLine;
        if (line == null) continue;

        sampleLines.Add(new {
            name = line.Name,
            groupName = group.Name,
            handle = line.Handle.ToString(),
            stationStart = line.StationStart,
            stationEnd = line.StationEnd,
            sectionCount = line.GetSectionIds().Count
        });
    }
}

return new { count = sampleLines.Count, sampleLines };
```

## Usage Notes
- Sample lines define cross-section locations along alignments
- Section views are generated from sample lines
- Used for earthwork and material quantity analysis
