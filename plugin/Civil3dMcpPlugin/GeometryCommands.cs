using System.Text.Json.Nodes;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace Civil3DMcpPlugin;

/// <summary>
/// Handles basic AutoCAD geometry creation: lines, polylines, 3D polylines, text.
/// </summary>
public static class GeometryCommands
{
  public static Task<object?> CreateLineSegmentAsync(JsonObject? parameters)
  {
    var startX = PluginRuntime.GetRequiredDouble(parameters, "startX");
    var startY = PluginRuntime.GetRequiredDouble(parameters, "startY");
    var startZ = PluginRuntime.GetOptionalDouble(parameters, "startZ") ?? 0.0;
    var endX = PluginRuntime.GetRequiredDouble(parameters, "endX");
    var endY = PluginRuntime.GetRequiredDouble(parameters, "endY");
    var endZ = PluginRuntime.GetOptionalDouble(parameters, "endZ") ?? 0.0;
    var layer = PluginRuntime.GetOptionalString(parameters, "layer");

    return CivilExecution.WriteAsync<object?>((doc, civilDoc, db, tr) =>
    {
      var line = new Line(
        new Point3d(startX, startY, startZ),
        new Point3d(endX, endY, endZ)
      );

      if (layer != null) line.Layer = layer;

      var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
      var btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
      btr.AppendEntity(line);
      tr.AddNewlyCreatedDBObject(line, true);

      return new
      {
        success = true,
        handle = line.Handle.ToString(),
        objectType = "Line",
      };
    });
  }

  public static Task<object?> CreatePolylineAsync(JsonObject? parameters)
  {
    var verticesNode = parameters?["vertices"] as JsonArray;
    var closed = PluginRuntime.GetOptionalBool(parameters, "closed") ?? false;
    var layer = PluginRuntime.GetOptionalString(parameters, "layer");

    if (verticesNode == null || verticesNode.Count < 2)
      throw new JsonRpcDispatchException("CIVIL3D.INVALID_INPUT", "At least 2 vertices are required.");

    return CivilExecution.WriteAsync<object?>((doc, civilDoc, db, tr) =>
    {
      var polyline = new Polyline();

      for (int i = 0; i < verticesNode.Count; i++)
      {
        var v = verticesNode[i]!;
        polyline.AddVertexAt(i,
          new Point2d(v["x"]!.GetValue<double>(), v["y"]!.GetValue<double>()),
          0, 0, 0);
      }

      polyline.Closed = closed;
      if (layer != null) polyline.Layer = layer;

      var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
      var btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
      btr.AppendEntity(polyline);
      tr.AddNewlyCreatedDBObject(polyline, true);

      return new
      {
        success = true,
        handle = polyline.Handle.ToString(),
        objectType = "Polyline",
        vertexCount = verticesNode.Count,
      };
    });
  }

  public static Task<object?> Create3dPolylineAsync(JsonObject? parameters)
  {
    var verticesNode = parameters?["vertices"] as JsonArray;
    var closed = PluginRuntime.GetOptionalBool(parameters, "closed") ?? false;
    var layer = PluginRuntime.GetOptionalString(parameters, "layer");

    if (verticesNode == null || verticesNode.Count < 2)
      throw new JsonRpcDispatchException("CIVIL3D.INVALID_INPUT", "At least 2 vertices are required.");

    return CivilExecution.WriteAsync<object?>((doc, civilDoc, db, tr) =>
    {
      var points = new Point3dCollection();
      foreach (var v in verticesNode)
      {
        points.Add(new Point3d(
          v!["x"]!.GetValue<double>(),
          v["y"]!.GetValue<double>(),
          v["z"]!.GetValue<double>()
        ));
      }

      var polyline3d = new Polyline3d(Poly3dType.SimplePoly, points, closed);
      if (layer != null) polyline3d.Layer = layer;

      var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
      var btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
      btr.AppendEntity(polyline3d);
      tr.AddNewlyCreatedDBObject(polyline3d, true);

      return new
      {
        success = true,
        handle = polyline3d.Handle.ToString(),
        objectType = "Polyline3d",
        vertexCount = verticesNode.Count,
      };
    });
  }

  public static Task<object?> CreateTextAsync(JsonObject? parameters)
  {
    var text = PluginRuntime.GetRequiredString(parameters, "text");
    var x = PluginRuntime.GetRequiredDouble(parameters, "insertionX");
    var y = PluginRuntime.GetRequiredDouble(parameters, "insertionY");
    var height = PluginRuntime.GetOptionalDouble(parameters, "height") ?? 1.0;
    var rotation = PluginRuntime.GetOptionalDouble(parameters, "rotation") ?? 0.0;
    var layer = PluginRuntime.GetOptionalString(parameters, "layer");

    return CivilExecution.WriteAsync<object?>((doc, civilDoc, db, tr) =>
    {
      var dbText = new DBText
      {
        TextString = text,
        Position = new Point3d(x, y, 0),
        Height = height,
        Rotation = rotation * Math.PI / 180.0,
      };

      if (layer != null) dbText.Layer = layer;

      var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
      var btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
      btr.AppendEntity(dbText);
      tr.AddNewlyCreatedDBObject(dbText, true);

      return new
      {
        success = true,
        handle = dbText.Handle.ToString(),
        objectType = "DBText",
      };
    });
  }

  public static Task<object?> CreateMTextAsync(JsonObject? parameters)
  {
    var text = PluginRuntime.GetRequiredString(parameters, "text");
    var x = PluginRuntime.GetRequiredDouble(parameters, "insertionX");
    var y = PluginRuntime.GetRequiredDouble(parameters, "insertionY");
    var height = PluginRuntime.GetOptionalDouble(parameters, "height") ?? 1.0;
    var layer = PluginRuntime.GetOptionalString(parameters, "layer");

    return CivilExecution.WriteAsync<object?>((doc, civilDoc, db, tr) =>
    {
      var mtext = new MText
      {
        Contents = text,
        Location = new Point3d(x, y, 0),
        TextHeight = height,
      };

      if (layer != null) mtext.Layer = layer;

      var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
      var btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
      btr.AppendEntity(mtext);
      tr.AddNewlyCreatedDBObject(mtext, true);

      return new
      {
        success = true,
        handle = mtext.Handle.ToString(),
        objectType = "MText",
      };
    });
  }
}
