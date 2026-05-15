using System.Text.Json.Nodes;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.Civil.DatabaseServices;

namespace Civil3DMcpPlugin;

/// <summary>
/// Handles COGO point operations: list, get, create, delete, groups, import.
/// </summary>
public static class PointCommands
{
  public static Task<object?> ListCogoPointsAsync(JsonObject? parameters)
  {
    var groupName = PluginRuntime.GetOptionalString(parameters, "groupName");
    var limit = PluginRuntime.GetOptionalInt(parameters, "limit") ?? 500;

    return CivilExecution.ReadAsync<object?>((doc, civilDoc, db, tr) =>
    {
      var cogoPoints = civilDoc.CogoPoints;
      var points = new List<object>();
      var count = 0;

      foreach (ObjectId id in cogoPoints)
      {
        if (count >= limit) break;

        var point = tr.GetObject(id, OpenMode.ForRead) as CogoPoint;
        if (point == null) continue;

        points.Add(new
        {
          pointNumber = point.PointNumber,
          easting = point.Easting,
          northing = point.Northing,
          elevation = point.Elevation,
          rawDescription = point.RawDescription,
          fullDescription = point.FullDescription,
        });
        count++;
      }

      return new { points, total = cogoPoints.Count };
    });
  }

  public static Task<object?> GetCogoPointAsync(JsonObject? parameters)
  {
    var pointNumber = PluginRuntime.GetRequiredInt(parameters, "pointNumber");

    return CivilExecution.ReadAsync<object?>((doc, civilDoc, db, tr) =>
    {
      var cogoPoints = civilDoc.CogoPoints;

      foreach (ObjectId id in cogoPoints)
      {
        var point = tr.GetObject(id, OpenMode.ForRead) as CogoPoint;
        if (point != null && point.PointNumber == (uint)pointNumber)
        {
          return new
          {
            pointNumber = point.PointNumber,
            easting = point.Easting,
            northing = point.Northing,
            elevation = point.Elevation,
            rawDescription = point.RawDescription,
            fullDescription = point.FullDescription,
            handle = point.Handle.ToString(),
            layer = point.Layer,
          };
        }
      }

      throw new JsonRpcDispatchException("CIVIL3D.NOT_FOUND", $"COGO point #{pointNumber} not found.");
    });
  }

  public static Task<object?> CreateCogoPointsAsync(JsonObject? parameters)
  {
    var pointsNode = parameters?["points"] as JsonArray;

    if (pointsNode == null || pointsNode.Count == 0)
      throw new JsonRpcDispatchException("CIVIL3D.INVALID_INPUT", "Parameter 'points' is required.");

    return CivilExecution.WriteAsync<object?>((doc, civilDoc, db, tr) =>
    {
      var cogoPoints = civilDoc.CogoPoints;
      var created = new List<object>();

      foreach (var pt in pointsNode)
      {
        var easting = pt!["easting"]!.GetValue<double>();
        var northing = pt["northing"]!.GetValue<double>();
        var elevation = pt["elevation"]?.GetValue<double>() ?? 0.0;
        var description = pt["rawDescription"]?.GetValue<string>() ?? "";

        var location = new Point3d(easting, northing, elevation);
        var pointId = cogoPoints.Add(location, false);
        var point = tr.GetObject(pointId, OpenMode.ForWrite) as CogoPoint;

        if (point != null && !string.IsNullOrEmpty(description))
        {
          point.RawDescription = description;
        }

        created.Add(new
        {
          pointNumber = point?.PointNumber ?? 0,
          easting,
          northing,
          elevation,
        });
      }

      return new { success = true, created };
    });
  }

  public static Task<object?> DeleteCogoPointsAsync(JsonObject? parameters)
  {
    var pointNumbersNode = parameters?["pointNumbers"] as JsonArray;

    if (pointNumbersNode == null || pointNumbersNode.Count == 0)
      throw new JsonRpcDispatchException("CIVIL3D.INVALID_INPUT", "Parameter 'pointNumbers' is required.");

    return CivilExecution.WriteAsync<object?>((doc, civilDoc, db, tr) =>
    {
      var cogoPoints = civilDoc.CogoPoints;
      var deleted = new List<int>();

      foreach (var pn in pointNumbersNode)
      {
        var pointNumber = pn!.GetValue<int>();

        foreach (ObjectId id in cogoPoints)
        {
          var point = tr.GetObject(id, OpenMode.ForRead) as CogoPoint;
          if (point != null && point.PointNumber == (uint)pointNumber)
          {
            point.UpgradeOpen();
            point.Erase();
            deleted.Add(pointNumber);
            break;
          }
        }
      }

      return new { success = true, deleted };
    });
  }

  public static Task<object?> ListPointGroupsAsync()
  {
    return CivilExecution.ReadAsync<object?>((doc, civilDoc, db, tr) =>
    {
      var groups = new List<object>();
      var pointGroupIds = civilDoc.PointGroups;

      foreach (ObjectId id in pointGroupIds)
      {
        var group = tr.GetObject(id, OpenMode.ForRead) as PointGroup;
        if (group == null) continue;

        groups.Add(new
        {
          name = group.Name,
          handle = group.Handle.ToString(),
          pointCount = group.PointsCount,
          description = group.Description,
        });
      }

      return new { groups };
    });
  }

  public static Task<object?> ImportCogoPointsAsync(JsonObject? parameters)
  {
    // Placeholder — requires PointFileFormat and file I/O
    return Task.FromResult<object?>(new
    {
      status = "planned",
      message = "importCogoPoints requires PointFileFormat configuration. Not yet fully implemented.",
    });
  }
}
