using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.Civil.ApplicationServices;
using Autodesk.Civil.DatabaseServices;

namespace Civil3DMcpPlugin;

/// <summary>
/// Globals exposed to Roslyn scripts. Properties and helpers are accessible
/// directly by name in the C# code sent by the AI.
/// </summary>
public class ScriptContext
{
  public Document Document { get; }
  public CivilDocument CivilDoc { get; }
  public Database Database { get; }
  public Transaction Transaction { get; }
  public Editor Editor { get; }

  /// <summary>Shorthand for Document.</summary>
  public Document Doc => Document;

  /// <summary>Shorthand for CivilDoc.</summary>
  public CivilDocument Civil => CivilDoc;

  /// <summary>Shorthand for Transaction.</summary>
  public Transaction Tr => Transaction;

  public ScriptContext(
    Document document,
    CivilDocument civilDoc,
    Database database,
    Transaction transaction)
  {
    Document = document;
    CivilDoc = civilDoc;
    Database = database;
    Transaction = transaction;
    Editor = document.Editor;
  }

  // ── Object lookup helpers ──

  public TinSurface? GetSurfaceByName(string name, OpenMode mode = OpenMode.ForRead)
  {
    foreach (ObjectId id in CivilDoc.GetSurfaceIds())
    {
      var surface = Transaction.GetObject(id, mode) as TinSurface;
      if (surface != null && surface.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
        return surface;
    }

    return null;
  }

  public Alignment? GetAlignmentByName(string name, OpenMode mode = OpenMode.ForRead)
  {
    foreach (ObjectId id in CivilDoc.GetAlignmentIds())
    {
      var alignment = Transaction.GetObject(id, mode) as Alignment;
      if (alignment != null && alignment.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
        return alignment;
    }

    return null;
  }

  public Profile? GetProfileByName(string alignmentName, string profileName, OpenMode mode = OpenMode.ForRead)
  {
    var alignment = GetAlignmentByName(alignmentName, OpenMode.ForRead);
    if (alignment == null) return null;

    foreach (ObjectId id in alignment.GetProfileIds())
    {
      var profile = Transaction.GetObject(id, mode) as Profile;
      if (profile != null && profile.Name.Equals(profileName, StringComparison.OrdinalIgnoreCase))
        return profile;
    }

    return null;
  }

  public CogoPoint? GetCogoPointByNumber(int number, OpenMode mode = OpenMode.ForRead)
  {
    foreach (ObjectId id in CivilDoc.CogoPoints)
    {
      var point = Transaction.GetObject(id, mode) as CogoPoint;
      if (point != null && point.PointNumber == number)
        return point;
    }

    return null;
  }

  public ObjectId? GetObjectIdByHandle(string handleHex)
  {
    if (!long.TryParse(handleHex, System.Globalization.NumberStyles.HexNumber, null, out var handleValue))
      return null;

    var handle = new Handle(handleValue);
    if (!Database.TryGetObjectId(handle, out var objectId) || objectId.IsNull)
      return null;

    return objectId;
  }

  public Autodesk.AutoCAD.DatabaseServices.DBObject? GetObjectByHandle(string handleHex, OpenMode mode = OpenMode.ForRead)
  {
    var objectId = GetObjectIdByHandle(handleHex);
    if (objectId == null || objectId.Value.IsNull)
      return null;

    return Transaction.GetObject(objectId.Value, mode);
  }

  // ── Inventory helpers (return JSON-friendly summaries) ──

  public List<object> ListSurfaces()
  {
    var items = new List<object>();
    foreach (ObjectId id in CivilDoc.GetSurfaceIds())
    {
      var surface = Transaction.GetObject(id, OpenMode.ForRead) as TinSurface;
      if (surface == null) continue;

      items.Add(new
      {
        name = surface.Name,
        handle = surface.Handle.ToString(),
        layer = surface.Layer,
      });
    }

    return items;
  }

  public List<object> ListAlignments()
  {
    var items = new List<object>();
    foreach (ObjectId id in CivilDoc.GetAlignmentIds())
    {
      var alignment = Transaction.GetObject(id, OpenMode.ForRead) as Alignment;
      if (alignment == null) continue;

      items.Add(new
      {
        name = alignment.Name,
        handle = alignment.Handle.ToString(),
        length = alignment.Length,
        layer = alignment.Layer,
      });
    }

    return items;
  }

  public List<object> ListCogoPoints(int limit = 100)
  {
    var items = new List<object>();
    var count = 0;

    foreach (ObjectId id in CivilDoc.CogoPoints)
    {
      if (count >= limit) break;

      var point = Transaction.GetObject(id, OpenMode.ForRead) as CogoPoint;
      if (point == null) continue;

      var location = point.Location;
      items.Add(new
      {
        number = point.PointNumber,
        rawDescription = point.RawDescription,
        fullDescription = point.FullDescription,
        handle = point.Handle.ToString(),
        easting = point.Easting,
        northing = point.Northing,
        elevation = point.Elevation,
        x = location.X,
        y = location.Y,
        z = location.Z,
      });
      count++;
    }

    return items;
  }

  /// <summary>Convert ObjectId to a JSON-friendly reference.</summary>
  public object ToRef(ObjectId id)
    => ResultSerializer.Serialize(id)!;

  /// <summary>Convert Point3d to a JSON-friendly object.</summary>
  public object ToPoint(Point3d point)
    => ResultSerializer.Serialize(point)!;

  // ── Safe file I/O (respects export policy) ──

  /// <summary>Allowed export/import directories for the current security policy.</summary>
  public IReadOnlyList<string> AllowedExportPaths => FileExportPolicy.GetAllowedPaths();

  /// <summary>Write text to a file under an allowed export directory.</summary>
  public string WriteExportFile(string path, string content)
  {
    var resolved = FileExportPolicy.ResolveAndValidate(path, forWrite: true);
    File.WriteAllText(resolved, content);
    return resolved;
  }

  /// <summary>Write lines to a CSV/text file under an allowed export directory.</summary>
  public string WriteExportLines(string path, IEnumerable<string> lines)
  {
    var resolved = FileExportPolicy.ResolveAndValidate(path, forWrite: true);
    File.WriteAllLines(resolved, lines);
    return resolved;
  }

  /// <summary>Read text from a file under an allowed import directory.</summary>
  public string ReadImportFile(string path)
  {
    var resolved = FileExportPolicy.ResolveAndValidate(path, forWrite: false);
    return File.ReadAllText(resolved);
  }
}
