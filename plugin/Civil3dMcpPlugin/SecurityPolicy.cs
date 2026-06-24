namespace Civil3DMcpPlugin;

/// <summary>Sandbox security level.</summary>
public enum SandboxMode
{
  /// <summary>No file IO. Maximum restrictions.</summary>
  Strict,
  /// <summary>File IO only to allowed export folders (default).</summary>
  Professional,
  /// <summary>Minimal restrictions — full Civil 3D access, external escapes still blocked.</summary>
  Unlocked,
}

/// <summary>
/// Central security configuration read from environment variables.
/// </summary>
public static class SecurityPolicy
{
  public static SandboxMode Mode => ParseMode(Environment.GetEnvironmentVariable("CIVIL3D_SANDBOX_MODE"));

  public static bool RequireConfirmation =>
    ParseBool(Environment.GetEnvironmentVariable("CIVIL3D_REQUIRE_CONFIRMATION"), defaultValue: true);

  public static IReadOnlyList<string> AllowedExportPaths => FileExportPolicy.GetAllowedPaths();

  public static object Describe()
  {
    return new
    {
      sandboxMode = Mode.ToString().ToLowerInvariant(),
      requireConfirmation = RequireConfirmation,
      allowedExportPaths = AllowedExportPaths,
      environmentVariables = new
      {
        sandboxMode = "CIVIL3D_SANDBOX_MODE (strict | professional | unlocked)",
        requireConfirmation = "CIVIL3D_REQUIRE_CONFIRMATION (true | false)",
        allowedExportPaths = "CIVIL3D_ALLOWED_EXPORT_PATHS (semicolon-separated paths)",
      },
    };
  }

  private static SandboxMode ParseMode(string? value)
  {
    return value?.Trim().ToLowerInvariant() switch
    {
      "strict" => SandboxMode.Strict,
      "unlocked" => SandboxMode.Unlocked,
      _ => SandboxMode.Professional,
    };
  }

  private static bool ParseBool(string? value, bool defaultValue)
  {
    if (string.IsNullOrWhiteSpace(value)) return defaultValue;
    return value.Trim().ToLowerInvariant() switch
    {
      "1" or "true" or "yes" or "on" => true,
      "0" or "false" or "no" or "off" => false,
      _ => defaultValue,
    };
  }
}
