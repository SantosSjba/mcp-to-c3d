---
name: list_parcels
category: parcels
description: List all parcels across all sites with area and perimeter
requires_write: false
parameters:
  - name: siteName
    type: string
    required: false
    description: Filter by site name (optional)
---

## Code Template

```csharp
var parcels = new List<object>();
var filterSite = "SITE_NAME_HERE"; // leave empty string to list all

foreach (ObjectId siteId in CivilDoc.GetSiteIds())
{
    var site = Transaction.GetObject(siteId, OpenMode.ForRead) as Site;
    if (site == null) continue;

    if (!string.IsNullOrEmpty(filterSite) &&
        !site.Name.Equals(filterSite, StringComparison.OrdinalIgnoreCase))
        continue;

    foreach (ObjectId parcelId in site.GetParcelIds())
    {
        var parcel = Transaction.GetObject(parcelId, OpenMode.ForRead) as Parcel;
        if (parcel == null) continue;

        parcels.Add(new {
            name = parcel.Name,
            siteName = site.Name,
            handle = parcel.Handle.ToString(),
            area = parcel.Area,
            perimeter = parcel.Perimeter,
            style = parcel.StyleName
        });
    }
}

return new { count = parcels.Count, parcels };
```

## Usage Notes
- Set filterSite to empty string to list parcels from all sites
- Area is in drawing units squared (sq m or sq ft)
- Parcels can be used for grading targets and volume calculations
