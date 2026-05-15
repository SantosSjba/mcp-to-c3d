using System.Text.Json.Nodes;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.Civil.DatabaseServices;

namespace Civil3DMcpPlugin;

/// <summary>
/// Handles profile operations: list, get, elevation, create from surface, layout, delete.
/// </summary>
public static class ProfileCommands
{
  public static Task<object?> ListProfilesAsync(JsonObject? parameters)
  {
    var alignmentName = PluginRuntime.GetRequiredString(parameters, "alignmentName");

    return CivilExecution.ReadAsync<object?>((doc, civilDoc, db, tr) =>
    {
      var alignment = AlignmentCommands.FindAlignmentByName(civilDoc, tr, alignmentName);
      var profiles = new List<object>();

      foreach (ObjectId id in alignment.GetProfileIds())
      {
        var profile = tr.GetObject(id, OpenMode.ForRead) as Profile;
        if (profile == null) continue;

        profiles.Add(new
        {
          name = profile.Name,
          handle = profile.Handle.ToString(),
          type = profile.ProfileType.ToString(),
          layer = profile.Layer,
        });
      }

      return new { alignmentName, profiles };
    });
  }

  public static Task<object?> GetProfileAsync(JsonObject? parameters)
  {
    var alignmentName = PluginRuntime.GetRequiredString(parameters, "alignmentName");
    var name = PluginRuntime.GetRequiredString(parameters, "name");

    return CivilExecution.ReadAsync<object?>((doc, civilDoc, db, tr) =>
    {
      var alignment = AlignmentCommands.FindAlignmentByName(civilDoc, tr, alignmentName);
      var profile = FindProfileByName(alignment, tr, name);

      return new
      {
        name = profile.Name,
        handle = profile.Handle.ToString(),
        type = profile.ProfileType.ToString(),
        startStation = profile.StartingStation,
        endStation = profile.EndingStation,
        layer = profile.Layer,
        style = profile.StyleName,
      };
    });
  }

  public static Task<object?> GetProfileElevationAsync(JsonObject? parameters)
  {
    var alignmentName = PluginRuntime.GetRequiredString(parameters, "alignmentName");
    var name = PluginRuntime.GetRequiredString(parameters, "name");
    var station = PluginRuntime.GetRequiredDouble(parameters, "station");

    return CivilExecution.ReadAsync<object?>((doc, civilDoc, db, tr) =>
    {
      var alignment = AlignmentCommands.FindAlignmentByName(civilDoc, tr, alignmentName);
      var profile = FindProfileByName(alignment, tr, name);

      var elevation = profile.ElevationAt(station);

      return new
      {
        alignmentName,
        profileName = name,
        station,
        elevation,
      };
    });
  }

  public static Task<object?> CreateProfileFromSurfaceAsync(JsonObject? parameters)
  {
    // Placeholder — requires alignment + surface IDs and profile style
    var alignmentName = PluginRuntime.GetRequiredString(parameters, "alignmentName");
    var surfaceName = PluginRuntime.GetRequiredString(parameters, "surfaceName");

    return Task.FromResult<object?>(new
    {
      status = "planned",
      message = $"createProfileFromSurface for alignment '{alignmentName}' + surface '{surfaceName}' is not yet fully implemented.",
    });
  }

  public static Task<object?> CreateLayoutProfileAsync(JsonObject? parameters)
  {
    // Placeholder — requires proper profile creation via the API
    return Task.FromResult<object?>(new
    {
      status = "planned",
      message = "createLayoutProfile is not yet fully implemented.",
    });
  }

  public static Task<object?> DeleteProfileAsync(JsonObject? parameters)
  {
    var alignmentName = PluginRuntime.GetRequiredString(parameters, "alignmentName");
    var name = PluginRuntime.GetRequiredString(parameters, "name");

    return CivilExecution.WriteAsync<object?>((doc, civilDoc, db, tr) =>
    {
      var alignment = AlignmentCommands.FindAlignmentByName(civilDoc, tr, alignmentName);
      var profile = FindProfileByName(alignment, tr, name);
      profile.UpgradeOpen();
      profile.Erase();

      return new { success = true, deleted = name };
    });
  }

  // ── Helpers ──

  private static Profile FindProfileByName(Alignment alignment, Transaction tr, string name)
  {
    foreach (ObjectId id in alignment.GetProfileIds())
    {
      var profile = tr.GetObject(id, OpenMode.ForRead) as Profile;
      if (profile != null && string.Equals(profile.Name, name, StringComparison.OrdinalIgnoreCase))
      {
        return profile;
      }
    }

    throw new JsonRpcDispatchException("CIVIL3D.NOT_FOUND", $"Profile '{name}' not found on alignment '{alignment.Name}'.");
  }
}
