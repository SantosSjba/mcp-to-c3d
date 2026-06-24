---
name: volume_between_surfaces
category: quantity
description: Compute cut/fill volume between two TIN surfaces
requires_write: false
parameters:
  - name: baseSurfaceName
    type: string
    required: true
    description: Base (existing ground) surface name
  - name: comparisonSurfaceName
    type: string
    required: true
    description: Comparison (design) surface name
---

## Code Template

```csharp
var baseSurface = GetSurfaceByName("BASE_SURFACE_NAME");
var compSurface = GetSurfaceByName("COMPARISON_SURFACE_NAME");

if (baseSurface == null) return new { error = "Base surface not found" };
if (compSurface == null) return new { error = "Comparison surface not found" };

// Create a volume surface for computation
var volumeSurfaceId = TinVolumeSurface.Create(
    Database,
    "Vol_" + baseSurface.Name + "_vs_" + compSurface.Name,
    baseSurface.ObjectId,
    compSurface.ObjectId
);

var volumeSurface = Transaction.GetObject(volumeSurfaceId, OpenMode.ForWrite) as TinVolumeSurface;
if (volumeSurface == null) return new { error = "Failed to create volume surface" };

var stats = volumeSurface.GetGeneralProperties();

return new {
    baseSurface = baseSurface.Name,
    comparisonSurface = compSurface.Name,
    volumeSurfaceName = volumeSurface.Name,
    cutVolume = stats.CutVolume,
    fillVolume = stats.FillVolume,
    netVolume = stats.NetVolume,
    note = "Volume surface created in drawing — delete if not needed"
};
```

## Usage Notes
- Creates a temporary TinVolumeSurface in the drawing (requires_write: true in practice)
- For read-only volume query without creating objects, use existing volume surfaces
- Volumes are in cubic drawing units (m³ or ft³)
