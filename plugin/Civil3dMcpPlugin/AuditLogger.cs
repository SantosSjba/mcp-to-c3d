using System.Text.Json;

namespace Civil3DMcpPlugin;

/// <summary>
/// Append-only audit log of MCP operations (JSON Lines format).
/// </summary>
public static class AuditLogger
{
  private static readonly object Sync = new();
  private static readonly int MaxEntries = GetMaxEntries();

  private static string LogPath =>
    Environment.GetEnvironmentVariable("CIVIL3D_AUDIT_LOG")
    ?? Path.Combine(
      Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
      "Civil3dMcp",
      "audit.jsonl"
    );

  public static void LogSuccess(string method, string? description, long durationMs)
  {
    WriteEntry(new AuditEntry
    {
      TimestampUtc = DateTime.UtcNow,
      Method = method,
      Description = description,
      Success = true,
      DurationMs = durationMs,
    });
  }

  public static void LogFailure(string method, string? description, long durationMs, string errorCode, string message)
  {
    WriteEntry(new AuditEntry
    {
      TimestampUtc = DateTime.UtcNow,
      Method = method,
      Description = description,
      Success = false,
      DurationMs = durationMs,
      ErrorCode = errorCode,
      ErrorMessage = message,
    });
  }

  public static object GetRecentEntries(int limit)
  {
    var entries = ReadEntries(Math.Clamp(limit, 1, 500));
    return new
    {
      logPath = LogPath,
      count = entries.Count,
      entries,
    };
  }

  private static void WriteEntry(AuditEntry entry)
  {
    try
    {
      lock (Sync)
      {
        var directory = Path.GetDirectoryName(LogPath);
        if (!string.IsNullOrEmpty(directory))
          Directory.CreateDirectory(directory);

        var line = JsonSerializer.Serialize(entry, JsonOptions);
        File.AppendAllText(LogPath, line + Environment.NewLine);
        TrimIfNeeded();
      }
    }
    catch
    {
      // Audit logging must never break operations.
    }
  }

  private static void TrimIfNeeded()
  {
    if (!File.Exists(LogPath)) return;

    var lines = File.ReadAllLines(LogPath);
    if (lines.Length <= MaxEntries) return;

    var trimmed = lines.Skip(lines.Length - MaxEntries).ToArray();
    File.WriteAllLines(LogPath, trimmed);
  }

  private static List<AuditEntry> ReadEntries(int limit)
  {
    try
    {
      if (!File.Exists(LogPath)) return new List<AuditEntry>();

      var lines = File.ReadAllLines(LogPath);
      return lines
        .Skip(Math.Max(0, lines.Length - limit))
        .Select(line =>
        {
          try { return JsonSerializer.Deserialize<AuditEntry>(line, JsonOptions); }
          catch { return null; }
        })
        .Where(e => e != null)
        .Select(e => e!)
        .ToList();
    }
    catch
    {
      return new List<AuditEntry>();
    }
  }

  private static int GetMaxEntries()
  {
    var env = Environment.GetEnvironmentVariable("CIVIL3D_AUDIT_MAX_ENTRIES");
    return int.TryParse(env, out var max) && max > 0 ? max : 1000;
  }

  private static readonly JsonSerializerOptions JsonOptions = new()
  {
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
  };

  private sealed class AuditEntry
  {
    public DateTime TimestampUtc { get; set; }
    public string Method { get; set; } = "";
    public string? Description { get; set; }
    public bool Success { get; set; }
    public long DurationMs { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
  }
}
