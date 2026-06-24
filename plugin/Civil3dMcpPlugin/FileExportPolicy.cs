namespace Civil3DMcpPlugin;

/// <summary>
/// Validates file paths against allowed export/import directories.
/// </summary>
public static class FileExportPolicy
{
  private static readonly object Sync = new();
  private static string[]? _cachedPaths;

  public static IReadOnlyList<string> GetAllowedPaths()
  {
    lock (Sync)
    {
      _cachedPaths ??= BuildAllowedPaths();
      return _cachedPaths;
    }
  }

  /// <summary>
  /// Resolve a path and verify it is under an allowed directory.
  /// Relative paths are resolved under the default exports folder.
  /// </summary>
  public static string ResolveAndValidate(string path, bool forWrite)
  {
    if (string.IsNullOrWhiteSpace(path))
    {
      throw new JsonRpcDispatchException(
        "CIVIL3D.FILE_POLICY_VIOLATION",
        "File path cannot be empty."
      );
    }

    var resolved = ResolvePath(path);
    var allowed = GetAllowedPaths();

    if (!IsUnderAllowedRoot(resolved, allowed))
    {
      throw new JsonRpcDispatchException(
        "CIVIL3D.FILE_POLICY_VIOLATION",
        $"Path '{resolved}' is not under an allowed export directory.",
        new
        {
          path = resolved,
          allowedExportPaths = allowed,
          hint = "Use WriteExportFile()/ReadImportFile() or add the folder to CIVIL3D_ALLOWED_EXPORT_PATHS.",
        }
      );
    }

    if (forWrite)
    {
      var directory = Path.GetDirectoryName(resolved);
      if (!string.IsNullOrEmpty(directory))
        Directory.CreateDirectory(directory);
    }
    else if (!File.Exists(resolved))
    {
      throw new JsonRpcDispatchException(
        "CIVIL3D.FILE_NOT_FOUND",
        $"File not found: {resolved}"
      );
    }

    return resolved;
  }

  public static void InvalidateCache()
  {
    lock (Sync) { _cachedPaths = null; }
  }

  private static string ResolvePath(string path)
  {
    if (Path.IsPathRooted(path))
      return Path.GetFullPath(path);

    var defaultExport = GetDefaultExportDirectory();
    return Path.GetFullPath(Path.Combine(defaultExport, path));
  }

  private static string GetDefaultExportDirectory()
  {
    var dir = Path.Combine(
      Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
      "Civil3dMcp",
      "Exports"
    );
    Directory.CreateDirectory(dir);
    return dir;
  }

  private static string[] BuildAllowedPaths()
  {
    var paths = new List<string>
    {
      GetDefaultExportDirectory(),
      Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
      Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
    };

    var extra = Environment.GetEnvironmentVariable("CIVIL3D_ALLOWED_EXPORT_PATHS");
    if (!string.IsNullOrWhiteSpace(extra))
    {
      foreach (var part in extra.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
      {
        try { paths.Add(Path.GetFullPath(part)); }
        catch { /* skip invalid paths */ }
      }
    }

    return paths
      .Where(p => !string.IsNullOrWhiteSpace(p))
      .Select(Path.GetFullPath)
      .Distinct(StringComparer.OrdinalIgnoreCase)
      .ToArray();
  }

  private static bool IsUnderAllowedRoot(string fullPath, IReadOnlyList<string> allowedRoots)
  {
    var normalized = Path.GetFullPath(fullPath);

    foreach (var root in allowedRoots)
    {
      if (normalized.StartsWith(root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
          || string.Equals(normalized, root, StringComparison.OrdinalIgnoreCase))
      {
        return true;
      }
    }

    return false;
  }
}
