---
name: create_corridor_workflow
category: workflows
description: Step-by-step workflow to create a corridor from alignment, profile, and assembly
requires_write: true
parameters:
  - name: alignmentName
    type: string
    required: true
  - name: profileName
    type: string
    required: true
  - name: assemblyName
    type: string
    required: true
  - name: corridorName
    type: string
    required: true
---

## Workflow Steps

### Step 1 — Discover what exists
```
civil3d_discover({ categories: ["alignments", "profiles", "styles"] })
```

### Step 2 — Verify alignment and profile
Use `list_profiles` skill filtered by alignment name.

### Step 3 — Create corridor via session (multi-step)

**begin session:**
```
civil3d_session({ action: "begin" })
```

**execute — create corridor:**
```csharp
// In sessionExecute:
var alignment = GetAlignmentByName("ALIGNMENT_NAME");
if (alignment == null) return new { error = "Alignment not found" };

// Find profile
Profile profile = null;
foreach (ObjectId profId in alignment.GetProfileIds())
{
    var p = Transaction.GetObject(profId, OpenMode.ForRead) as Profile;
    if (p != null && p.Name.Equals("PROFILE_NAME", StringComparison.OrdinalIgnoreCase))
    { profile = p; break; }
}
if (profile == null) return new { error = "Profile not found" };

// Create corridor
var corridorId = Corridor.Create("CORRIDOR_NAME", alignment.ObjectId);
var corridor = Transaction.GetObject(corridorId, OpenMode.ForWrite) as Corridor;

// Add baseline with profile
var baseline = corridor.Baselines.Add(alignment.Name, alignment.ObjectId);
baseline.ProfileId = profile.ObjectId;

return new { success = true, corridorName = corridor.Name, handle = corridor.Handle.ToString() };
```

**commit session:**
```
civil3d_session({ action: "commit", sessionId: "..." })
```

### Step 4 — Add assembly region via command
```
civil3d_command({ command: "AeccAddCorridorAssembly", waitForCompletion: true })
```

### Step 5 — Rebuild
```
civil3d_command({ command: "AeccCorridorRebuild", waitForCompletion: true, timeoutMs: 300000 })
```

## Usage Notes
- Assembly must exist in the drawing (check with `list_object_styles`)
- Corridor creation API varies by Civil 3D version — use `civil3d_command` as fallback
- Always rebuild after adding regions
