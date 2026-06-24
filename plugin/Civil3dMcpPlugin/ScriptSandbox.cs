using System.Text.RegularExpressions;

namespace Civil3DMcpPlugin;

/// <summary>
/// Security sandbox that validates C# code before execution.
/// Behavior depends on <see cref="SecurityPolicy.Mode"/>.
/// </summary>
public static class ScriptSandbox
{
  private static readonly (string Pattern, string Reason)[] AlwaysBlocked =
  {
    (@"System\.Diagnostics\.Process", "Process execution is not allowed"),
    (@"System\.Net\.Http", "HTTP requests are not allowed"),
    (@"System\.Net\.Sockets", "Socket operations are not allowed"),
    (@"System\.Reflection\.Assembly\.Load", "Dynamic assembly loading is not allowed"),
    (@"System\.Runtime\.InteropServices", "P/Invoke is not allowed"),
    (@"Environment\.Exit", "Process termination is not allowed"),
    (@"Registry\.", "Registry access is not allowed"),
    (@"Process\.Start", "Process execution is not allowed"),
    (@"AppDomain\.CreateDomain", "Creating AppDomains is not allowed"),
  };

  private static readonly (string Pattern, string Reason)[] ProfessionalBlocked =
  {
    (@"System\.IO\.File\.Delete", "File deletion is not allowed — use Civil 3D APIs instead"),
    (@"System\.IO\.Directory\.Delete", "Directory deletion is not allowed"),
  };

  private static readonly string FileIoPattern =
    @"System\.IO\.File\.(Write|Append|Create|Open|Copy|Move|Replace|Read)";

  /// <summary>
  /// Validate code against sandbox rules and destructive-operation policy.
  /// </summary>
  public static void Validate(string code, bool confirmed = false, bool readOnly = false)
  {
    if (string.IsNullOrWhiteSpace(code))
    {
      throw new JsonRpcDispatchException(
        "CIVIL3D.INVALID_INPUT",
        "Code cannot be empty."
      );
    }

    var mode = SecurityPolicy.Mode;

    ValidatePatterns(code, AlwaysBlocked);

    switch (mode)
    {
      case SandboxMode.Strict:
        ValidatePatterns(code, ProfessionalBlocked);
        if (Regex.IsMatch(code, FileIoPattern, RegexOptions.IgnoreCase)
            || Regex.IsMatch(code, @"System\.IO\.Directory\.", RegexOptions.IgnoreCase))
        {
          throw new JsonRpcDispatchException(
            "CIVIL3D.SANDBOX_VIOLATION",
            "File system access is not allowed in strict sandbox mode. " +
              "Use WriteExportFile()/ReadImportFile() or set CIVIL3D_SANDBOX_MODE=professional.",
            new { sandboxMode = "strict" }
          );
        }
        break;

      case SandboxMode.Professional:
        ValidatePatterns(code, ProfessionalBlocked);
        ValidateFilePathsInCode(code);
        break;

      case SandboxMode.Unlocked:
        // Only always-blocked patterns apply. File IO allowed without path checks.
        if (Regex.IsMatch(code, @"System\.IO\.File\.Delete", RegexOptions.IgnoreCase)
            || Regex.IsMatch(code, @"System\.IO\.Directory\.Delete", RegexOptions.IgnoreCase))
        {
          // Deletion still triggers confirmation, not hard block in unlocked mode.
        }
        break;
    }

    if (!readOnly)
    {
      var destructiveReasons = DestructiveOperationDetector.DetectInCode(code);
      DestructiveOperationDetector.RequireConfirmationIfNeeded(destructiveReasons, confirmed, "script");
    }
  }

  private static void ValidatePatterns(string code, (string Pattern, string Reason)[] patterns)
  {
    foreach (var (pattern, reason) in patterns)
    {
      if (Regex.IsMatch(code, pattern, RegexOptions.IgnoreCase))
      {
        throw new JsonRpcDispatchException(
          "CIVIL3D.SANDBOX_VIOLATION",
          $"Script blocked: {reason}.",
          new { pattern, reason, sandboxMode = SecurityPolicy.Mode.ToString().ToLowerInvariant() }
        );
      }
    }
  }

  private static void ValidateFilePathsInCode(string code)
  {
    if (!Regex.IsMatch(code, FileIoPattern, RegexOptions.IgnoreCase))
      return;

    // Allow safe helper methods on ScriptContext
    if (Regex.IsMatch(code, @"\bWriteExportFile\s*\(", RegexOptions.IgnoreCase)
        || Regex.IsMatch(code, @"\bReadImportFile\s*\(", RegexOptions.IgnoreCase))
    {
      // Helpers validate paths at runtime — still check raw File.* calls below.
    }

    var pathLiterals = ExtractStringLiteralsNearFileCalls(code);
    if (pathLiterals.Count == 0)
    {
      throw new JsonRpcDispatchException(
        "CIVIL3D.FILE_POLICY_VIOLATION",
        "Direct File I/O with dynamic paths is not allowed in professional mode. " +
          "Use WriteExportFile(path, content) or ReadImportFile(path), or use a string literal path under allowed folders.",
        new
        {
          allowedExportPaths = FileExportPolicy.GetAllowedPaths(),
          sandboxMode = "professional",
        }
      );
    }

    var violations = new List<string>();
    foreach (var literal in pathLiterals)
    {
      try
      {
        FileExportPolicy.ResolveAndValidate(literal, forWrite: true);
      }
      catch (JsonRpcDispatchException ex) when (ex.Code == "CIVIL3D.FILE_POLICY_VIOLATION")
      {
        violations.Add(literal);
      }
    }

    if (violations.Count > 0)
    {
      throw new JsonRpcDispatchException(
        "CIVIL3D.FILE_POLICY_VIOLATION",
        "One or more file paths in the script are outside allowed export directories.",
        new
        {
          blockedPaths = violations,
          allowedExportPaths = FileExportPolicy.GetAllowedPaths(),
        }
      );
    }
  }

  private static List<string> ExtractStringLiteralsNearFileCalls(string code)
  {
    var results = new List<string>();

    // Match File.Method(@"path" or "path" or variable — we only accept literals
    var matches = Regex.Matches(
      code,
      @"System\.IO\.File\.\w+\s*\(\s*(@""(?:[^""]|"""")*""|""(?:\\.|[^""])*"")",
      RegexOptions.IgnoreCase);

    foreach (Match match in matches)
    {
      if (match.Groups.Count < 2) continue;
      var raw = match.Groups[1].Value;
      results.Add(UnquoteCSharpString(raw));
    }

    return results;
  }

  private static string UnquoteCSharpString(string literal)
  {
    if (literal.StartsWith("@\"", StringComparison.Ordinal))
      return literal[2..^1].Replace("\"\"", "\"");

    if (literal.StartsWith('"'))
      return literal[1..^1].Replace("\\\"", "\"").Replace("\\\\", "\\");

    return literal;
  }
}
