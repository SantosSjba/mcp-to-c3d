---
name: site_parcel_workflow
category: workflows
description: Workflow for site creation, parcel management, and grading analysis
requires_write: false
parameters: []
---

## Workflow Steps

### Step 1 — Discover sites and parcels
```
civil3d_discover({ categories: ["sites", "parcels", "surfaces"] })
```

### Step 2 — List parcels with areas
Use `list_parcels` skill (no site filter for full inventory).

### Step 3 — Analyze parcel grading
```csharp
// In civil3d_query — get parcel details for grading review:
var parcels = new List<object>();
foreach (ObjectId siteId in CivilDoc.GetSiteIds())
{
    var site = Transaction.GetObject(siteId, OpenMode.ForRead) as Site;
    if (site == null) continue;

    foreach (ObjectId parcelId in site.GetParcelIds())
    {
        var parcel = Transaction.GetObject(parcelId, OpenMode.ForRead) as Parcel;
        if (parcel == null) continue;

        parcels.Add(new {
            site = site.Name,
            parcel = parcel.Name,
            area = parcel.Area,
            perimeter = parcel.Perimeter,
            minElevation = parcel.GetMinimumElevation(),
            maxElevation = parcel.GetMaximumElevation()
        });
    }
}
return new { count = parcels.Count, parcels };
```

### Step 4 — Create site (write, via command)
```
civil3d_command({ command: "CreateSite", waitForCompletion: true })
```

### Step 5 — Volume analysis
Use `volume_between_surfaces` for cut/fill between EG and FG surfaces within parcel boundaries.

## Usage Notes
- Parcels belong to sites — always check site context
- Grading groups may be associated with parcels
- Area values use drawing unit system
