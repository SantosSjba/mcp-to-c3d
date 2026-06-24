---
name: list_sites
category: parcels
description: List all sites with parcel counts
requires_write: false
parameters: []
---

## Code Template

```csharp
var sites = new List<object>();

foreach (ObjectId id in CivilDoc.GetSiteIds())
{
    var site = Transaction.GetObject(id, OpenMode.ForRead) as Site;
    if (site == null) continue;

    sites.Add(new {
        name = site.Name,
        handle = site.Handle.ToString(),
        parcelCount = site.GetParcelIds().Count
    });
}

return new { count = sites.Count, sites };
```

## Usage Notes
- Sites group parcels for grading and layout
- Use `list_parcels` to see all parcels across sites
- Parcels are created within a site context
