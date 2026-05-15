using System.Text.Json.Nodes;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.Civil.DatabaseServices;

namespace Civil3DMcpPlugin;

/// <summary>
/// Handles surface operations: list, get, elevation, statistics, create, delete,
/// add points/breaklines/boundaries, contours, and volume computation.
/// </summary>
public static class SurfaceCommands
{
  public static Task<object?> ListSurfacesAsync()
  {
    return CivilExecution.ReadAsync<object?>((doc, civilDoc, db, tr) =>
    {
      var surfaceIds = civilDoc.GetSurfaceIds();
      var surfaces = new List<object>();

      foreach (ObjectId id in surfaceIds)
      {
        var surface = tr.GetObject(id, OpenMode.ForRead) as Autodesk.Civil.DatabaseServices.Surface;
        if (surface == null) continue;

        surfaces.Add(new
        {
          name = surface.Name,
          handle = surface.Handle.ToString(),
          type = surface is TinSurface ? "TIN" : surface is GridSurface ? "Grid" : "Other",
        });
      }

      return new { surfaces };
    });
  }

  public static Task<object?> GetSurfaceAsync(JsonObject? parameters)
  {
    var name = PluginRuntime.GetRequiredString(parameters, "name");

    return CivilExecution.ReadAsync<object?>((doc, civilDoc, db, tr) =>
    {
      var surface = FindSurfaceByName(civilDoc, tr, name);

      var result = new
      {
        name = surface.Name,
        handle = surface.Handle.ToString(),
        type = surface is TinSurface ? "TIN" : surface is GridSurface ? "Grid" : "Other",
        layer = surface.Layer,
        style = surface.StyleName,
      };

      if (surface is TinSurface tinSurface)
      {
        return new
        {
          result.name,
          result.handle,
          result.type,
          result.layer,
          result.style,
          statistics = new
          {
            minimumElevation = tinSurface.GetGeneralProperties().MinimumElevation,
            maximumElevation = tinSurface.GetGeneralProperties().MaximumElevation,
            numberOfPoints = tinSurface.GetGeneralProperties().NumberOfPoints,
          },
        };
      }

      return (object)result;
    });
  }

  public static Task<object?> GetSurfaceElevationAsync(JsonObject? parameters)
  {
    var name = PluginRuntime.GetRequiredString(parameters, "name");
    var x = PluginRuntime.GetRequiredDouble(parameters, "x");
    var y = PluginRuntime.GetRequiredDouble(parameters, "y");

    return CivilExecution.ReadAsync<object?>((doc, civilDoc, db, tr) =>
    {
      var surface = FindSurfaceByName(civilDoc, tr, name);
      var elevation = surface.FindElevationAtXY(x, y);

      return new
      {
        surfaceName = name,
        elevation,
        x,
        y,
      };
    });
  }

  public static Task<object?> GetSurfaceStatisticsAsync(JsonObject? parameters)
  {
    var name = PluginRuntime.GetRequiredString(parameters, "name");

    return CivilExecution.ReadAsync<object?>((doc, civilDoc, db, tr) =>
    {
      var surface = FindSurfaceByName(civilDoc, tr, name);
      var props = surface.GetGeneralProperties();

      return new
      {
        surfaceName = name,
        minimumElevation = props.MinimumElevation,
        maximumElevation = props.MaximumElevation,
        numberOfPoints = props.NumberOfPoints,
      };
    });
  }

  public static Task<object?> CreateSurfaceAsync(JsonObject? parameters)
  {
    var name = PluginRuntime.GetRequiredString(parameters, "name");
    var style = PluginRuntime.GetOptionalString(parameters, "style");
    var layer = PluginRuntime.GetOptionalString(parameters, "layer");
    var description = PluginRuntime.GetOptionalString(parameters, "description");

    return CivilExecution.WriteAsync<object?>((doc, civilDoc, db, tr) =>
    {
      var surfaceId = TinSurface.Create(db, name);
      var surface = tr.GetObject(surfaceId, OpenMode.ForWrite) as TinSurface;

      if (description != null && surface != null)
      {
        surface.Description = description;
      }

      return new
      {
        success = true,
        name,
        handle = surface?.Handle.ToString(),
      };
    });
  }

  public static Task<object?> DeleteSurfaceAsync(JsonObject? parameters)
  {
    var name = PluginRuntime.GetRequiredString(parameters, "name");

    return CivilExecution.WriteAsync<object?>((doc, civilDoc, db, tr) =>
    {
      var surface = FindSurfaceByName(civilDoc, tr, name);
      surface.UpgradeOpen();
      surface.Erase();

      return new { success = true, deleted = name };
    });
  }

  public static Task<object?> AddSurfacePointsAsync(JsonObject? parameters)
  {
    var name = PluginRuntime.GetRequiredString(parameters, "name");
    var pointsNode = parameters?["points"] as JsonArray;

    if (pointsNode == null || pointsNode.Count == 0)
      throw new JsonRpcDispatchException("CIVIL3D.INVALID_INPUT", "Parameter 'points' is required.");

    return CivilExecution.WriteAsync<object?>((doc, civilDoc, db, tr) =>
    {
      var surface = FindSurfaceByName(civilDoc, tr, name) as TinSurface
        ?? throw new JsonRpcDispatchException("CIVIL3D.INVALID_INPUT", $"Surface '{name}' is not a TIN surface.");

      surface.UpgradeOpen();
      var points = new Point3dCollection();

      foreach (var pt in pointsNode)
      {
        var x = pt!["x"]!.GetValue<double>();
        var y = pt["y"]!.GetValue<double>();
        var z = pt["z"]!.GetValue<double>();
        points.Add(new Point3d(x, y, z));
      }

      surface.AddVertices(points);

      return new { success = true, surfaceName = name, pointsAdded = points.Count };
    });
  }

  public static Task<object?> AddSurfaceBreaklineAsync(JsonObject? parameters)
  {
    // Placeholder — requires more complex implementation
    return Task.FromResult<object?>(new { status = "planned", message = "addSurfaceBreakline is not yet fully implemented." });
  }

  public static Task<object?> AddSurfaceBoundaryAsync(JsonObject? parameters)
  {
    // Placeholder — requires more complex implementation
    return Task.FromResult<object?>(new { status = "planned", message = "addSurfaceBoundary is not yet fully implemented." });
  }

  public static Task<object?> ExtractSurfaceContoursAsync(JsonObject? parameters)
  {
    // Placeholder — requires more complex implementation
    return Task.FromResult<object?>(new { status = "planned", message = "extractSurfaceContours is not yet fully implemented." });
  }

  public static Task<object?> ComputeSurfaceVolumeAsync(JsonObject? parameters)
  {
    var baseName = PluginRuntime.GetRequiredString(parameters, "baseSurface");
    var compName = PluginRuntime.GetRequiredString(parameters, "comparisonSurface");

    return CivilExecution.ReadAsync<object?>((doc, civilDoc, db, tr) =>
    {
      var baseSurface = FindSurfaceByName(civilDoc, tr, baseName) as TinSurface
        ?? throw new JsonRpcDispatchException("CIVIL3D.INVALID_INPUT", $"'{baseName}' is not a TIN surface.");
      var compSurface = FindSurfaceByName(civilDoc, tr, compName) as TinSurface
        ?? throw new JsonRpcDispatchException("CIVIL3D.INVALID_INPUT", $"'{compName}' is not a TIN surface.");

      var props = baseSurface.GetVolumeProperties(compSurface);

      return new
      {
        baseSurface = baseName,
        comparisonSurface = compName,
        cutVolume = props.UnadjustedCutVolume,
        fillVolume = props.UnadjustedFillVolume,
        netVolume = props.UnadjustedCutVolume - props.UnadjustedFillVolume,
      };
    });
  }

  // ── Helpers ──

  private static Autodesk.Civil.DatabaseServices.Surface FindSurfaceByName(
    CivilDocument civilDoc, Transaction tr, string name)
  {
    foreach (ObjectId id in civilDoc.GetSurfaceIds())
    {
      var surface = tr.GetObject(id, OpenMode.ForRead) as Autodesk.Civil.DatabaseServices.Surface;
      if (surface != null && string.Equals(surface.Name, name, StringComparison.OrdinalIgnoreCase))
      {
        return surface;
      }
    }

    throw new JsonRpcDispatchException("CIVIL3D.NOT_FOUND", $"Surface '{name}' not found.");
  }
}
