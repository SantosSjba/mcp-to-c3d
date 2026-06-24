---
name: list_object_styles
category: styles
description: List counts of all major Civil 3D object styles in the drawing
requires_write: false
parameters: []
---

## Code Template

```csharp
var styles = CivilDoc.Styles;

return new {
    surfaceStyles = styles.SurfaceStyles.Count,
    alignmentStyles = styles.AlignmentStyles.Count,
    corridorStyles = styles.CorridorStyles.Count,
    profileStyles = styles.ProfileStyles.Count,
    sectionStyles = styles.SectionStyles.Count,
    pipeStyles = styles.PipeStyles.Count,
    structureStyles = styles.StructureStyles.Count,
    parcelStyles = styles.ParcelStyles.Count,
    labelStyles = styles.LabelStyles.Count,
    markerStyles = styles.MarkerStyles.Count,
    materialStyles = styles.MaterialStyles.Count,
    assemblyStyles = styles.AssemblyStyles.Count
};
```

## Usage Notes
- Styles define display and behavior of Civil 3D objects
- Object styles are referenced when creating surfaces, alignments, corridors, etc.
- Use `civil3d_discover` with category `styles` for a quick overview
