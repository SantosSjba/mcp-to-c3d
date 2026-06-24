---
name: profile_elevation_at_station
category: profiles
description: Get profile elevation at a specific station on an alignment
requires_write: false
parameters:
  - name: alignmentName
    type: string
    required: true
  - name: profileName
    type: string
    required: true
  - name: station
    type: double
    required: true
---

## Code Template

```csharp
var alignment = GetAlignmentByName("ALIGNMENT_NAME_HERE");
if (alignment == null) return new { error = "Alignment not found" };

Profile profile = null;
foreach (ObjectId profId in alignment.GetProfileIds())
{
    var p = Transaction.GetObject(profId, OpenMode.ForRead) as Profile;
    if (p != null && p.Name.Equals("PROFILE_NAME_HERE", StringComparison.OrdinalIgnoreCase))
    { profile = p; break; }
}
if (profile == null) return new { error = "Profile not found" };

double station = 0.0; // STATION_VALUE_HERE
double elevation = profile.ElevationAt(station);

return new {
    alignmentName = alignment.Name,
    profileName = profile.Name,
    station,
    elevation,
    profileType = profile.ProfileType.ToString()
};
```

## Usage Notes
- Station must be within the profile's station range
- Works for surface and layout profiles
- Combine with `station_offset` for 3D point queries
