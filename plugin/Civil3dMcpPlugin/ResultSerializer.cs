using System.Collections;
using System.Reflection;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace Civil3DMcpPlugin;

/// <summary>
/// Converts script return values into JSON-safe structures.
/// Handles AutoCAD types (ObjectId, Handle, Point3d, etc.) consistently.
/// </summary>
public static class ResultSerializer
{
  private const int MaxDepth = 12;
  private const int MaxCollectionItems = 500;

  public static object? Serialize(object? value)
    => Serialize(value, 0, new HashSet<object>(ReferenceEqualityComparer.Instance));

  private static object? Serialize(object? value, int depth, HashSet<object> visited)
  {
    if (value == null) return null;
    if (depth > MaxDepth) return "<max depth>";

    switch (value)
    {
      case string s:
        return s;
      case bool b:
        return b;
      case byte or sbyte or short or ushort or int or uint or long or ulong:
        return Convert.ToInt64(value);
      case float or double or decimal:
        return Convert.ToDouble(value);
      case Enum e:
        return e.ToString();
      case ObjectId objectId:
        return SerializeObjectId(objectId);
      case Handle handle:
        return handle.ToString();
      case Point2d p2:
        return new { x = p2.X, y = p2.Y };
      case Point3d p3:
        return new { x = p3.X, y = p3.Y, z = p3.Z };
      case Vector2d v2:
        return new { x = v2.X, y = v2.Y };
      case Vector3d v3:
        return new { x = v3.X, y = v3.Y, z = v3.Z };
      case Autodesk.AutoCAD.DatabaseServices.DBObject dbObject:
        return SerializeDbObject(dbObject);
      case IDictionary dictionary:
        return SerializeDictionary(dictionary, depth, visited);
      case IEnumerable enumerable when value is not string:
        return SerializeEnumerable(enumerable, depth, visited);
    }

    var type = value.GetType();
    if (type.IsPrimitive) return value;

    if (!type.IsValueType)
    {
      if (visited.Contains(value)) return "<cycle>";
      visited.Add(value);
    }

    // Anonymous types and plain POCOs
    if (type.Name.Contains("AnonymousType", StringComparison.Ordinal) || HasReadableProperties(type))
      return SerializeObjectProperties(value, depth, visited);

    return value.ToString();
  }

  private static object SerializeObjectId(ObjectId objectId)
  {
    if (objectId.IsNull)
      return new { isNull = true, isValid = false };

    return new
    {
      isNull = false,
      isValid = objectId.IsValid,
      handle = objectId.Handle.ToString(),
      database = objectId.Database?.Filename,
    };
  }

  private static object SerializeDbObject(Autodesk.AutoCAD.DatabaseServices.DBObject dbObject)
  {
    string? layer = dbObject is Entity entity ? entity.Layer : null;

    return new
    {
      type = dbObject.GetType().Name,
      handle = dbObject.Handle.ToString(),
      layer,
      objectId = SerializeObjectId(dbObject.ObjectId),
    };
  }

  private static object SerializeDictionary(IDictionary dictionary, int depth, HashSet<object> visited)
  {
    var result = new Dictionary<string, object?>();
    foreach (DictionaryEntry entry in dictionary)
    {
      result[entry.Key?.ToString() ?? ""] = Serialize(entry.Value, depth + 1, visited);
    }

    return result;
  }

  private static object SerializeEnumerable(IEnumerable enumerable, int depth, HashSet<object> visited)
  {
    var items = new List<object?>();
    var count = 0;

    foreach (var item in enumerable)
    {
      if (count >= MaxCollectionItems)
      {
        items.Add($"<truncated at {MaxCollectionItems} items>");
        break;
      }

      items.Add(Serialize(item, depth + 1, visited));
      count++;
    }

    return items;
  }

  private static object SerializeObjectProperties(object value, int depth, HashSet<object> visited)
  {
    var result = new Dictionary<string, object?>();
    var properties = value.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);

    foreach (var property in properties)
    {
      if (!property.CanRead || property.GetIndexParameters().Length > 0)
        continue;

      object? propertyValue;
      try
      {
        propertyValue = property.GetValue(value);
      }
      catch
      {
        result[property.Name] = "<unreadable>";
        continue;
      }

      result[property.Name] = Serialize(propertyValue, depth + 1, visited);
    }

    return result;
  }

  private static bool HasReadableProperties(Type type)
  {
    return type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
      .Any(p => p.CanRead && p.GetIndexParameters().Length == 0);
  }
}
