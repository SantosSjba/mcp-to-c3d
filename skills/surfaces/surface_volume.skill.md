---
name: surface_volume
category: surfaces
description: Compute cut/fill volumes between two TIN surfaces
requires_write: false
parameters:
  - name: baseSurface
    type: string
    required: true
    description: Name of the base (existing ground) surface
  - name: comparisonSurface
    type: string
    required: true
    description: Name of the comparison (proposed) surface
---

## Code Template

```csharp
TinSurface baseSurf = null, compSurf = null;

foreach (ObjectId id in CivilDoc.GetSurfaceIds())
{
    var s = Transaction.GetObject(id, OpenMode.ForRead);
    if (s is TinSurface tin)
    {
        if (tin.Name.Equals("BASE_SURFACE_NAME", StringComparison.OrdinalIgnoreCase))
            baseSurf = tin;
        if (tin.Name.Equals("COMPARISON_SURFACE_NAME", StringComparison.OrdinalIgnoreCase))
            compSurf = tin;
    }
}

if (baseSurf == null) return new { error = "Base surface not found" };
if (compSurf == null) return new { error = "Comparison surface not found" };

var volumeProps = baseSurf.GetVolumeProperties(compSurf);

return new {
    baseSurface = baseSurf.Name,
    comparisonSurface = compSurf.Name,
    cutVolume = volumeProps.UnadjustedCutVolume,
    fillVolume = volumeProps.UnadjustedFillVolume,
    netVolume = volumeProps.UnadjustedCutVolume - volumeProps.UnadjustedFillVolume,
    cutArea = volumeProps.UnadjustedCutArea,
    fillArea = volumeProps.UnadjustedFillArea
};
```

## Usage Notes
- Both surfaces must be TIN surfaces
- Net volume positive = more cut than fill
- Surfaces must overlap for meaningful results
- Units depend on drawing settings (typically m³ or ft³)
