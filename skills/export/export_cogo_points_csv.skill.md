---
name: export_cogo_points_csv
category: export
description: Export COGO points to a CSV file on disk
requires_write: true
parameters:
  - name: filePath
    type: string
    required: true
    description: Full path for the CSV file (e.g. C:\Exports\points.csv)
  - name: limit
    type: int
    required: false
    description: Max points to export (default all)
---

## Code Template

```csharp
var filePath = @"C:\Exports\points.csv";
var lines = new List<string> { "PointNumber,Easting,Northing,Elevation,RawDescription,FullDescription" };

foreach (ObjectId id in CivilDoc.CogoPoints)
{
    var pt = Transaction.GetObject(id, OpenMode.ForRead) as CogoPoint;
    if (pt == null) continue;

    lines.Add(string.Join(",",
        pt.PointNumber,
        pt.Easting.ToString("F3"),
        pt.Northing.ToString("F3"),
        pt.Elevation.ToString("F3"),
        $"\"{pt.RawDescription}\"",
        $"\"{pt.FullDescription}\""
    ));
}

System.IO.File.WriteAllLines(filePath, lines);

return new {
    success = true,
    filePath,
    pointCount = lines.Count - 1
};
```

## Usage Notes
- Replace filePath with a valid writable path
- CSV uses drawing coordinate units
- For LandXML export, use `landxml_export` skill with `civil3d_command`
