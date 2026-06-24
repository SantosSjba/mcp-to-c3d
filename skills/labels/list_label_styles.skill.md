---
name: list_label_styles
category: labels
description: List available label style categories and counts in the drawing
requires_write: false
parameters: []
---

## Code Template

```csharp
var labelStyles = CivilDoc.Styles.LabelStyles;

return new {
    alignmentLabelStyles = labelStyles.AlignmentLabelStyles.Count,
    profileLabelStyles = labelStyles.ProfileLabelStyles.Count,
    sectionLabelStyles = labelStyles.SectionLabelStyles.Count,
    pipeNetworkLabelStyles = labelStyles.PipeNetworkLabelStyles.Count,
    parcelLabelStyles = labelStyles.ParcelLabelStyles.Count,
    surfaceLabelStyles = labelStyles.SurfaceLabelStyles.Count,
    generalLineLabelStyles = labelStyles.GeneralLineLabelStyles.Count,
    generalNoteLabelStyles = labelStyles.GeneralNoteLabelStyles.Count,
    total = labelStyles.Count
};
```

## Usage Notes
- Label styles control annotation appearance for Civil 3D objects
- To apply labels, typically use native commands or API label creation
- Use `list_object_styles` for non-label style inventories
