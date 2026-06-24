---
name: landxml_export
category: export
description: Export Civil 3D data to LandXML using native command
requires_write: false
parameters:
  - name: filePath
    type: string
    required: true
    description: Full path for the LandXML file
---

## Code Template

This skill uses `civil3d_command` — do NOT execute as C# directly.

## Command Sequence

Use `civil3d_command` with these commands in order:

```
1. civil3d_command({ command: "LANDXMLOUT", waitForCompletion: true, timeoutMs: 120000 })
```

Civil 3D will prompt for file path interactively. For automated export, use:

```
civil3d_command({ command: "._-LANDXMLOUT\n\"C:\\Exports\\project.xml\"\n", waitForCompletion: true })
```

## Alternative C# Approach

```csharp
// LandXML export typically requires the command line interface
// Return instructions for the AI to use civil3d_command
return new {
    instruction = "Use civil3d_command",
    command = "._-LANDXMLOUT",
    filePath = @"C:\Exports\project.xml",
    note = "LandXML export is best done via native command with file path"
};
```

## Usage Notes
- LandXML exports alignments, surfaces, parcels, pipe networks
- File path must be accessible and not locked
- Use forward slashes or escaped backslashes in command strings
