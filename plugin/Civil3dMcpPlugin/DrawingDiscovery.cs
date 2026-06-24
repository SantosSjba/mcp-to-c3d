using System.Collections;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.Civil.ApplicationServices;
using Autodesk.Civil.DatabaseServices;
using CivilSurface = Autodesk.Civil.DatabaseServices.Surface;

namespace Civil3DMcpPlugin;

/// <summary>
/// Read-only inventory of Civil 3D drawing objects for the discover tool.
/// Uses defensive API access for compatibility across Civil 3D versions.
/// </summary>
public static class DrawingDiscovery
{
  private static readonly HashSet<string> AllCategories = new(StringComparer.OrdinalIgnoreCase)
  {
    "summary", "surfaces", "alignments", "profiles", "corridors",
    "pipeNetworks", "sites", "parcels", "cogoPoints", "sampleLines", "styles",
  };

  public static object Run(
    CivilDocument civilDoc,
    Transaction transaction,
    string[]? categories,
    int limit)
  {
    var selected = ResolveCategories(categories);
    var result = new Dictionary<string, object?>();

    if (selected.Contains("summary"))
      result["summary"] = BuildSummary(civilDoc, transaction);

    if (selected.Contains("surfaces"))
      result["surfaces"] = DiscoverSurfaces(civilDoc, transaction, limit);

    if (selected.Contains("alignments"))
      result["alignments"] = DiscoverAlignments(civilDoc, transaction, limit);

    if (selected.Contains("profiles"))
      result["profiles"] = DiscoverProfiles(civilDoc, transaction, limit);

    if (selected.Contains("corridors"))
      result["corridors"] = DiscoverCorridors(civilDoc, transaction, limit);

    if (selected.Contains("pipeNetworks"))
      result["pipeNetworks"] = DiscoverPipeNetworks(civilDoc, transaction, limit);

    if (selected.Contains("sites"))
      result["sites"] = DiscoverSites(civilDoc, transaction, limit);

    if (selected.Contains("parcels"))
      result["parcels"] = DiscoverParcels(civilDoc, transaction, limit);

    if (selected.Contains("cogoPoints"))
      result["cogoPoints"] = DiscoverCogoPoints(civilDoc, transaction, limit);

    if (selected.Contains("sampleLines"))
      result["sampleLines"] = DiscoverSampleLines(civilDoc, transaction, limit);

    if (selected.Contains("styles"))
      result["styles"] = DiscoverStyles(civilDoc);

    return result;
  }

  private static HashSet<string> ResolveCategories(string[]? categories)
  {
    if (categories == null || categories.Length == 0)
      return new HashSet<string>(AllCategories, StringComparer.OrdinalIgnoreCase);

    var resolved = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    foreach (var category in categories)
    {
      if (AllCategories.Contains(category))
        resolved.Add(category);
    }

    return resolved.Count > 0
      ? resolved
      : new HashSet<string>(AllCategories, StringComparer.OrdinalIgnoreCase);
  }

  private static object BuildSummary(CivilDocument civilDoc, Transaction transaction)
  {
    return new
    {
      surfaces = civilDoc.GetSurfaceIds().Count,
      alignments = civilDoc.GetAlignmentIds().Count,
      corridors = CountObjectIds(CivilDocAccess.GetCorridorIds(civilDoc)),
      pipeNetworks = civilDoc.GetPipeNetworkIds().Count,
      sites = civilDoc.GetSiteIds().Count,
      cogoPoints = civilDoc.CogoPoints.Count,
      parcels = CountParcels(civilDoc, transaction),
      profiles = CountProfiles(civilDoc, transaction),
      sampleLines = CountSampleLines(civilDoc, transaction),
    };
  }

  private static object DiscoverSurfaces(CivilDocument civilDoc, Transaction tr, int limit)
  {
    var items = new List<object>();
    foreach (ObjectId id in civilDoc.GetSurfaceIds())
    {
      if (items.Count >= limit) break;

      var surface = tr.GetObject(id, OpenMode.ForRead) as CivilSurface;
      if (surface == null) continue;

      var item = new Dictionary<string, object?>
      {
        ["name"] = surface.Name,
        ["handle"] = surface.Handle.ToString(),
        ["layer"] = surface.Layer,
        ["style"] = surface.StyleName,
        ["type"] = surface is TinSurface ? "TIN" : surface is GridSurface ? "Grid" : "Other",
      };

      if (surface is TinSurface tin)
      {
        var props = tin.GetGeneralProperties();
        item["minElevation"] = props.MinimumElevation;
        item["maxElevation"] = props.MaximumElevation;
        item["numberOfPoints"] = props.NumberOfPoints;
      }

      items.Add(item);
    }

    return new { count = items.Count, total = civilDoc.GetSurfaceIds().Count, items };
  }

  private static object DiscoverAlignments(CivilDocument civilDoc, Transaction tr, int limit)
  {
    var items = new List<object>();
    foreach (ObjectId id in civilDoc.GetAlignmentIds())
    {
      if (items.Count >= limit) break;

      var alignment = tr.GetObject(id, OpenMode.ForRead) as Alignment;
      if (alignment == null) continue;

      items.Add(new
      {
        name = alignment.Name,
        handle = alignment.Handle.ToString(),
        layer = alignment.Layer,
        length = alignment.Length,
        startStation = alignment.StartingStation,
        endStation = alignment.EndingStation,
        profileCount = alignment.GetProfileIds().Count,
        style = alignment.StyleName,
      });
    }

    return new { count = items.Count, total = civilDoc.GetAlignmentIds().Count, items };
  }

  private static object DiscoverProfiles(CivilDocument civilDoc, Transaction tr, int limit)
  {
    var items = new List<object>();
    foreach (ObjectId alignId in civilDoc.GetAlignmentIds())
    {
      var alignment = tr.GetObject(alignId, OpenMode.ForRead) as Alignment;
      if (alignment == null) continue;

      foreach (ObjectId profId in alignment.GetProfileIds())
      {
        if (items.Count >= limit) break;

        var profile = tr.GetObject(profId, OpenMode.ForRead) as Profile;
        if (profile == null) continue;

        items.Add(new
        {
          name = profile.Name,
          alignmentName = alignment.Name,
          handle = profile.Handle.ToString(),
          type = profile.ProfileType.ToString(),
          startStation = profile.StartingStation,
          endStation = profile.EndingStation,
          minElevation = profile.ElevationMin,
          maxElevation = profile.ElevationMax,
        });
      }

      if (items.Count >= limit) break;
    }

    return new { count = items.Count, total = CountProfiles(civilDoc, tr), items };
  }

  private static object DiscoverCorridors(CivilDocument civilDoc, Transaction tr, int limit)
  {
    var items = new List<object>();
    var corridorIds = CivilDocAccess.GetCorridorIds(civilDoc);

    foreach (ObjectId id in corridorIds)
    {
      if (items.Count >= limit) break;

      var corridor = tr.GetObject(id, OpenMode.ForRead) as Corridor;
      if (corridor == null) continue;

      var regionCount = 0;
      foreach (Baseline baseline in corridor.Baselines)
        regionCount += baseline.BaselineRegions.Count;

      items.Add(new
      {
        name = corridor.Name,
        handle = corridor.Handle.ToString(),
        layer = corridor.Layer,
        style = corridor.StyleName,
        baselineCount = corridor.Baselines.Count,
        regionCount,
      });
    }

    return new { count = items.Count, total = CountObjectIds(corridorIds), items };
  }

  private static object DiscoverPipeNetworks(CivilDocument civilDoc, Transaction tr, int limit)
  {
    var items = new List<object>();
    foreach (ObjectId id in civilDoc.GetPipeNetworkIds())
    {
      if (items.Count >= limit) break;

      var network = tr.GetObject(id, OpenMode.ForRead) as Network;
      if (network == null) continue;

      items.Add(new
      {
        name = network.Name,
        handle = network.Handle.ToString(),
        layer = network.Layer,
        partCount = network.GetPipeIds().Count + network.GetStructureIds().Count,
        pipeCount = network.GetPipeIds().Count,
        structureCount = network.GetStructureIds().Count,
        referenceSurface = network.ReferenceSurfaceName,
      });
    }

    return new { count = items.Count, total = civilDoc.GetPipeNetworkIds().Count, items };
  }

  private static object DiscoverSites(CivilDocument civilDoc, Transaction tr, int limit)
  {
    var items = new List<object>();
    foreach (ObjectId id in civilDoc.GetSiteIds())
    {
      if (items.Count >= limit) break;

      var site = tr.GetObject(id, OpenMode.ForRead) as Site;
      if (site == null) continue;

      items.Add(new
      {
        name = site.Name,
        handle = site.Handle.ToString(),
        parcelCount = site.GetParcelIds().Count,
      });
    }

    return new { count = items.Count, total = civilDoc.GetSiteIds().Count, items };
  }

  private static object DiscoverParcels(CivilDocument civilDoc, Transaction tr, int limit)
  {
    var items = new List<object>();
    foreach (ObjectId siteId in civilDoc.GetSiteIds())
    {
      var site = tr.GetObject(siteId, OpenMode.ForRead) as Site;
      if (site == null) continue;

      foreach (ObjectId parcelId in site.GetParcelIds())
      {
        if (items.Count >= limit) break;

        var parcel = tr.GetObject(parcelId, OpenMode.ForRead) as Parcel;
        if (parcel == null) continue;

        items.Add(new
        {
          name = parcel.Name,
          siteName = site.Name,
          handle = parcel.Handle.ToString(),
          area = parcel.Area,
        });
      }

      if (items.Count >= limit) break;
    }

    return new { count = items.Count, total = CountParcels(civilDoc, tr), items };
  }

  private static object DiscoverCogoPoints(CivilDocument civilDoc, Transaction tr, int limit)
  {
    var items = new List<object>();
    foreach (ObjectId id in civilDoc.CogoPoints)
    {
      if (items.Count >= limit) break;

      var point = tr.GetObject(id, OpenMode.ForRead) as CogoPoint;
      if (point == null) continue;

      items.Add(new
      {
        number = point.PointNumber,
        rawDescription = point.RawDescription,
        easting = point.Easting,
        northing = point.Northing,
        elevation = point.Elevation,
        handle = point.Handle.ToString(),
      });
    }

    return new { count = items.Count, total = civilDoc.CogoPoints.Count, items };
  }

  private static object DiscoverSampleLines(CivilDocument civilDoc, Transaction tr, int limit)
  {
    var items = new List<object>();
    var groupIds = CivilDocAccess.GetSampleLineGroupIds(civilDoc);

    foreach (ObjectId groupId in groupIds)
    {
      var group = tr.GetObject(groupId, OpenMode.ForRead) as SampleLineGroup;
      if (group == null) continue;

      foreach (ObjectId lineId in CivilDocAccess.GetSampleLineIds(group))
      {
        if (items.Count >= limit) break;

        var sampleLine = tr.GetObject(lineId, OpenMode.ForRead) as SampleLine;
        if (sampleLine == null) continue;

        items.Add(new
        {
          name = sampleLine.Name,
          groupName = group.Name,
          handle = sampleLine.Handle.ToString(),
        });
      }

      if (items.Count >= limit) break;
    }

    return new { count = items.Count, total = CountSampleLines(civilDoc, tr), items };
  }

  private static object DiscoverStyles(CivilDocument civilDoc)
  {
    var styles = civilDoc.Styles;

    return new
    {
      surfaceStyles = styles.SurfaceStyles.Count,
      alignmentStyles = styles.AlignmentStyles.Count,
      corridorStyles = styles.CorridorStyles.Count,
      profileStyles = styles.ProfileStyles.Count,
      sectionStyles = styles.SectionStyles.Count,
      pipeStyles = styles.PipeStyles.Count,
      structureStyles = styles.StructureStyles.Count,
      parcelStyles = styles.ParcelStyles.Count,
      markerStyles = styles.MarkerStyles.Count,
    };
  }

  private static int CountProfiles(CivilDocument civilDoc, Transaction tr)
  {
    var count = 0;
    foreach (ObjectId alignId in civilDoc.GetAlignmentIds())
    {
      var alignment = tr.GetObject(alignId, OpenMode.ForRead) as Alignment;
      if (alignment != null)
        count += alignment.GetProfileIds().Count;
    }

    return count;
  }

  private static int CountParcels(CivilDocument civilDoc, Transaction tr)
  {
    var count = 0;
    foreach (ObjectId siteId in civilDoc.GetSiteIds())
    {
      var site = tr.GetObject(siteId, OpenMode.ForRead) as Site;
      if (site != null)
        count += site.GetParcelIds().Count;
    }

    return count;
  }

  private static int CountSampleLines(CivilDocument civilDoc, Transaction tr)
  {
    var count = 0;
    foreach (ObjectId groupId in CivilDocAccess.GetSampleLineGroupIds(civilDoc))
    {
      var group = tr.GetObject(groupId, OpenMode.ForRead) as SampleLineGroup;
      if (group != null)
        count += CountObjectIds(CivilDocAccess.GetSampleLineIds(group));
    }

    return count;
  }

  private static int CountObjectIds(IEnumerable ids)
  {
    var count = 0;
    foreach (var _ in ids) count++;
    return count;
  }
}

/// <summary>Version-tolerant accessors for Civil 3D document collections.</summary>
internal static class CivilDocAccess
{
  public static IEnumerable GetCorridorIds(CivilDocument civilDoc)
  {
    var result = InvokeObjectIdCollection(civilDoc, "GetCorridorIds");
    return result ?? Array.Empty<ObjectId>();
  }

  public static IEnumerable GetSampleLineGroupIds(CivilDocument civilDoc)
  {
    var result = InvokeObjectIdCollection(civilDoc, "GetSampleLineGroupIds");
    return result ?? Array.Empty<ObjectId>();
  }

  public static IEnumerable GetSampleLineIds(SampleLineGroup group)
  {
    var result = InvokeObjectIdCollection(group, "GetSampleLineIds");
    return result ?? Array.Empty<ObjectId>();
  }

  private static IEnumerable? InvokeObjectIdCollection(object target, string methodName)
  {
    try
    {
      var method = target.GetType().GetMethod(methodName);
      if (method == null) return null;
      return method.Invoke(target, null) as IEnumerable;
    }
    catch
    {
      return null;
    }
  }
}
