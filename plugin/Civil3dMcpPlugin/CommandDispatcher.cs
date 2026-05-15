using System.Text.Json.Nodes;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.Civil.ApplicationServices;
using App = Autodesk.AutoCAD.ApplicationServices.Application;

namespace Civil3DMcpPlugin;

/// <summary>
/// Routes JSON-RPC methods. With code execution architecture,
/// only 2 methods are needed: executeCode and listSkills.
/// </summary>
public static class CommandDispatcher
{
  public static async Task<object?> DispatchAsync(
    string method,
    JsonObject? parameters,
    CancellationToken cancellationToken)
  {
    return method switch
    {
      "executeCode" => await ExecuteCodeAsync(parameters),
      "getCivil3DHealth" => await GetHealthAsync(),

      _ => throw new JsonRpcDispatchException(
        "CIVIL3D.INVALID_INPUT",
        $"Unknown method '{method}'. Available: executeCode, getCivil3DHealth"
      ),
    };
  }

  /// <summary>
  /// Execute C# code via Roslyn in the Civil 3D context.
  /// </summary>
  private static async Task<object?> ExecuteCodeAsync(JsonObject? parameters)
  {
    var code = PluginRuntime.GetRequiredString(parameters, "code");
    var readOnly = parameters?["readOnly"]?.GetValue<bool>() ?? false;
    var description = PluginRuntime.GetOptionalString(parameters, "description") ?? "Script execution";

    // Log the execution
    System.Diagnostics.Debug.WriteLine($"[C3D-MCP] {(readOnly ? "QUERY" : "EXECUTE")}: {description}");

    // Execute on Civil 3D main thread with proper document locking
    return await CivilExecution.ExecuteAsync((doc, civilDoc, db, tr) =>
    {
      var context = new ScriptContext(doc, civilDoc, db, tr);

      // Run the Roslyn script synchronously within the command context
      // (we're already on the main thread here)
      var task = RoslynExecutor.ExecuteAsync(code, context);
      task.Wait(); // Safe because we're in ExecuteInCommandContextAsync

      return task.Result;
    }, write: !readOnly);
  }

  /// <summary>Health check — verifies the plugin is alive and Civil 3D is responsive.</summary>
  private static Task<object?> GetHealthAsync()
  {
    return CivilExecution.ReadAsync<object?>((doc, civilDoc, db, tr) =>
    {
      return new
      {
        connected = true,
        drawingName = doc.Name,
        mode = "code_execution",
        roslyn = true,
      };
    });
  }
}
