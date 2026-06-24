namespace Civil3DMcpPlugin;

/// <summary>
/// Tracks the currently running operation for status/progress reporting.
/// </summary>
public sealed record OperationStatus(
  bool IsIdle,
  string? Method,
  string? Description,
  DateTime? StartedAtUtc,
  long ElapsedMs,
  int QueueDepth,
  int ActiveSessions
);

/// <summary>
/// Thread-safe operation progress tracker.
/// </summary>
public static class OperationTracker
{
  private static readonly object Sync = new();
  private static string? _method;
  private static string? _description;
  private static DateTime? _startedAtUtc;

  public static void Begin(string method, string? description)
  {
    lock (Sync)
    {
      _method = method;
      _description = description ?? method;
      _startedAtUtc = DateTime.UtcNow;
    }
  }

  public static void End()
  {
    lock (Sync)
    {
      _method = null;
      _description = null;
      _startedAtUtc = null;
    }
  }

  public static OperationStatus GetStatus(int queueDepth, int activeSessions)
  {
    lock (Sync)
    {
      var isIdle = _startedAtUtc == null;
      var elapsed = _startedAtUtc.HasValue
        ? (long)(DateTime.UtcNow - _startedAtUtc.Value).TotalMilliseconds
        : 0L;

      return new OperationStatus(
        isIdle,
        _method,
        _description,
        _startedAtUtc,
        elapsed,
        queueDepth,
        activeSessions
      );
    }
  }
}
