using System.Text.RegularExpressions;

namespace Civil3DMcpPlugin;

/// <summary>
/// Detects potentially destructive operations in C# code and native commands.
/// </summary>
public static class DestructiveOperationDetector
{
  private static readonly (string Pattern, string Reason)[] CodePatterns =
  {
    (@"\bErase\s*\(", "Object erase"),
    (@"\.Erase\s*\(", "Entity erase"),
    (@"\bDeleteEntity\b", "Delete entity"),
    (@"\bWipeOut\b.*\bErase\b", "Wipeout erase"),
    (@"System\.IO\.File\.Delete", "File deletion"),
    (@"System\.IO\.Directory\.Delete", "Directory deletion"),
    (@"\bExplode\s*\(", "Explode entities"),
    (@"\bPurge\s*\(", "Purge drawing"),
    (@"\bRemove\s*\(.*Surface", "Remove surface"),
    (@"\bDelete\s*\(.*Surface", "Delete surface"),
    (@"\bRemove\s*\(.*Alignment", "Remove alignment"),
    (@"\bDelete\s*\(.*Alignment", "Delete alignment"),
    (@"\bRemove\s*\(.*Corridor", "Remove corridor"),
    (@"\bDelete\s*\(.*Corridor", "Delete corridor"),
    (@"OpenMode\.ForWrite.*\.Erase", "Erase in write mode"),
  };

  private static readonly (string Pattern, string Reason)[] CommandPatterns =
  {
    (@"^ERASE\b", "ERASE command"),
    (@"^-PURGE\b", "PURGE command"),
    (@"^PURGE\b", "PURGE command"),
    (@"^-OVERWRITE\b", "Overwrite save"),
    (@"^DEL\b", "Delete command"),
    (@"^RM\b", "Remove command"),
    (@"^WIPEOUT\b", "Wipeout command"),
    (@"^EXPLODE\b", "Explode command"),
    (@"^AeccDelete", "Civil 3D delete command"),
    (@"^AeccRemove", "Civil 3D remove command"),
  };

  public static IReadOnlyList<string> DetectInCode(string code)
  {
    var reasons = new List<string>();
    foreach (var (pattern, reason) in CodePatterns)
    {
      if (Regex.IsMatch(code, pattern, RegexOptions.IgnoreCase))
        reasons.Add(reason);
    }

    return reasons.Distinct().ToList();
  }

  public static IReadOnlyList<string> DetectInCommand(string command)
  {
    var trimmed = command.Trim();
    var reasons = new List<string>();

    foreach (var (pattern, reason) in CommandPatterns)
    {
      if (Regex.IsMatch(trimmed, pattern, RegexOptions.IgnoreCase))
        reasons.Add(reason);
    }

    return reasons.Distinct().ToList();
  }

  public static void RequireConfirmationIfNeeded(
    IReadOnlyList<string> reasons,
    bool confirmed,
    string operationType)
  {
    if (!SecurityPolicy.RequireConfirmation || reasons.Count == 0)
      return;

    if (confirmed)
      return;

    throw new JsonRpcDispatchException(
      "CIVIL3D.CONFIRMATION_REQUIRED",
      $"Destructive {operationType} requires explicit confirmation. " +
        "Review the operation and call again with confirmed: true.",
      new
      {
        destructiveReasons = reasons,
        operationType,
        hint = "Set confirmed: true on civil3d_execute, civil3d_session, or civil3d_command after user approval.",
      }
    );
  }
}
