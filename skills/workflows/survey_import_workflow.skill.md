---
name: survey_import_workflow
category: workflows
description: Workflow for importing survey data (points, surfaces) into Civil 3D
requires_write: true
parameters: []
---

## Workflow Steps

### Step 1 — Assess current drawing
```
civil3d_health()
civil3d_discover({ categories: ["summary", "cogoPoints", "surfaces"] })
```

### Step 2 — Import points from CSV (C# execute)
```csharp
// Read CSV and create COGO points
var filePath = @"C:\Survey\points.csv";
var lines = System.IO.File.ReadAllLines(filePath);
var created = new List<object>();

for (int i = 1; i < lines.Length; i++) // skip header
{
    var parts = lines[i].Split(',');
    if (parts.Length < 4) continue;

    var number = int.Parse(parts[0]);
    var easting = double.Parse(parts[1]);
    var northing = double.Parse(parts[2]);
    var elevation = double.Parse(parts[3]);
    var desc = parts.Length > 4 ? parts[4] : "";

    var pointId = CogoPoint.Create(CivilDoc, number);
    var point = Transaction.GetObject(pointId, OpenMode.ForWrite) as CogoPoint;
    point.Easting = easting;
    point.Northing = northing;
    point.Elevation = elevation;
    point.RawDescription = desc;

    created.Add(new { number, easting, northing, elevation });
}

return new { imported = created.Count, points = created };
```

### Step 3 — Create surface from points
Use `create_tin_surface` skill with the imported point coordinates.

### Step 4 — Verify surface
```
civil3d_discover({ categories: ["surfaces"] })
```

### Step 5 — Export confirmation
Use `export_cogo_points_csv` to write back and verify.

## Alternative: Native Import
```
civil3d_command({ command: "_.-IMPORT", waitForCompletion: true })
```

## Usage Notes
- CSV format: PointNumber,Easting,Northing,Elevation,Description
- Point numbers must be unique in the drawing
- After import, always verify with `civil3d_discover`
