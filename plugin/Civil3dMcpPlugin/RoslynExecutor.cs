using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

namespace Civil3DMcpPlugin;

/// <summary>
/// Compiles and executes C# code snippets using Roslyn.
/// Provides full access to AutoCAD + Civil 3D APIs through ScriptContext globals.
/// Includes script caching for performance.
/// </summary>
public static class RoslynExecutor
{
  /// <summary>Cache of compiled scripts by code hash.</summary>
  private static readonly ConcurrentDictionary<int, Script<object>> _scriptCache = new();

  /// <summary>Max script execution time (default 120 seconds).</summary>
  public static TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(120);

  /// <summary>
  /// Build ScriptOptions with all necessary references and imports.
  /// </summary>
  private static ScriptOptions BuildOptions()
  {
    // Collect assemblies from the current AppDomain (Civil 3D loads everything)
    var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies()
      .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
      .ToArray();

    var options = ScriptOptions.Default
      .WithReferences(loadedAssemblies)
      .WithImports(
        // System
        "System",
        "System.Linq",
        "System.Collections.Generic",
        "System.Text",
        // AutoCAD
        "Autodesk.AutoCAD.ApplicationServices",
        "Autodesk.AutoCAD.DatabaseServices",
        "Autodesk.AutoCAD.EditorInput",
        "Autodesk.AutoCAD.Geometry",
        "Autodesk.AutoCAD.Runtime",
        // Civil 3D
        "Autodesk.Civil",
        "Autodesk.Civil.ApplicationServices",
        "Autodesk.Civil.DatabaseServices",
        "Autodesk.Civil.Settings"
      )
      .WithAllowUnsafe(false);

    return options;
  }

  /// <summary>
  /// Execute a C# code snippet with the given ScriptContext as globals.
  /// </summary>
  /// <param name="code">C# code to execute</param>
  /// <param name="context">Globals (Document, CivilDoc, Database, Transaction, Editor)</param>
  /// <returns>The script's return value, or null</returns>
  public static async Task<object?> ExecuteAsync(string code, ScriptContext context)
  {
    // Validate with sandbox
    ScriptSandbox.Validate(code);

    var options = BuildOptions();
    var codeHash = code.GetHashCode();

    // Try cache first
    if (!_scriptCache.TryGetValue(codeHash, out var script))
    {
      script = CSharpScript.Create<object>(code, options, typeof(ScriptContext));
      script.Compile(); // Pre-compile for better error messages
      _scriptCache.TryAdd(codeHash, script);
    }

    // Execute with timeout
    using var cts = new CancellationTokenSource(Timeout);

    try
    {
      var result = await script.RunAsync(context, cts.Token);
      return result.ReturnValue;
    }
    catch (OperationCanceledException)
    {
      throw new JsonRpcDispatchException(
        "CIVIL3D.TIMEOUT",
        $"Script execution timed out after {Timeout.TotalSeconds}s."
      );
    }
    catch (CompilationErrorException ex)
    {
      var errors = string.Join("\n", ex.Diagnostics.Select(d => d.ToString()));
      throw new JsonRpcDispatchException(
        "CIVIL3D.COMPILATION_ERROR",
        $"C# compilation failed:\n{errors}"
      );
    }
  }

  /// <summary>Clear the script cache.</summary>
  public static void ClearCache()
  {
    _scriptCache.Clear();
  }
}
