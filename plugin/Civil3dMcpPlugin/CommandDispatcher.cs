using System.Diagnostics;
using System.Text.Json.Nodes;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.Civil.ApplicationServices;
using App = Autodesk.AutoCAD.ApplicationServices.Application;

namespace Civil3DMcpPlugin;

/// <summary>
/// Routes JSON-RPC methods to Civil 3D operations.
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
      "executeCode" => await ExecuteCodeAsync(parameters, cancellationToken),
      "executeNativeCommand" => await ExecuteNativeCommandAsync(parameters, cancellationToken),
      "beginSession" => await BeginSessionAsync(),
      "sessionExecute" => await SessionExecuteAsync(parameters, cancellationToken),
      "sessionCommit" => await SessionCommitAsync(parameters),
      "sessionAbort" => await SessionAbortAsync(parameters),
      "discoverDrawing" => await DiscoverDrawingAsync(parameters),
      "getCivil3DHealth" => await GetHealthAsync(),
      "getOperationStatus" => GetOperationStatus(),
      "getAuditLog" => GetAuditLog(parameters),
      "getSecurityPolicy" => GetSecurityPolicy(),

      _ => throw new JsonRpcDispatchException(
        "CIVIL3D.INVALID_INPUT",
        $"Unknown method '{method}'. Available: executeCode, executeNativeCommand, " +
        "beginSession, sessionExecute, sessionCommit, sessionAbort, discoverDrawing, " +
        "getCivil3DHealth, getOperationStatus, getAuditLog, getSecurityPolicy"
      ),
    };
  }

  private static async Task<object?> ExecuteCodeAsync(
    JsonObject? parameters,
    CancellationToken cancellationToken)
  {
    var code = PluginRuntime.GetRequiredString(parameters, "code");
    var readOnly = parameters?["readOnly"]?.GetValue<bool>() ?? false;
    var description = PluginRuntime.GetOptionalString(parameters, "description") ?? "Script execution";
    var timeoutMs = PluginRuntime.GetOperationTimeoutMs("executeCode", parameters);
    var timeout = TimeSpan.FromMilliseconds(timeoutMs);
    var confirmed = parameters?["confirmed"]?.GetValue<bool>() ?? false;

    Debug.WriteLine($"[C3D-MCP] {(readOnly ? "QUERY" : "EXECUTE")}: {description}");

    var raw = await CivilExecution.ExecuteAsync((doc, civilDoc, db, tr) =>
    {
      var context = new ScriptContext(doc, civilDoc, db, tr);
      var task = RoslynExecutor.ExecuteAsync(code, context, cancellationToken, timeout, confirmed, readOnly);
      task.Wait(cancellationToken);
      return task.Result;
    }, write: !readOnly);

    return ResultSerializer.Serialize(raw);
  }

  private static async Task<object?> ExecuteNativeCommandAsync(
    JsonObject? parameters,
    CancellationToken cancellationToken)
  {
    var command = PluginRuntime.GetRequiredString(parameters, "command");
    var confirmed = parameters?["confirmed"]?.GetValue<bool>() ?? false;
    var destructiveReasons = DestructiveOperationDetector.DetectInCommand(command);
    DestructiveOperationDetector.RequireConfirmationIfNeeded(destructiveReasons, confirmed, "command");

    var waitForCompletion = parameters?["waitForCompletion"]?.GetValue<bool>() ?? true;
    var timeoutMs = parameters?["timeoutMs"]?.GetValue<int>()
      ?? PluginRuntime.GetOperationTimeoutMs("executeNativeCommand", parameters);

    var result = await CivilExecution.RunOnMainThreadAsync((doc, civilDoc, db) =>
    {
      doc.SendStringToExecute(command, true, false, false);

      if (!waitForCompletion)
      {
        return (object?)new { success = true, command, completed = false };
      }

      var sw = Stopwatch.StartNew();
      while (IsCommandInProgress(doc) && sw.ElapsedMilliseconds < timeoutMs)
      {
        cancellationToken.ThrowIfCancellationRequested();
        Thread.Sleep(50);
      }

      var completed = !IsCommandInProgress(doc);
      if (completed)
      {
        CivilExecution.RefreshDrawing(doc);
      }

      return new
      {
        success = completed,
        command,
        completed,
        elapsedMs = sw.ElapsedMilliseconds,
        timedOut = !completed,
      };
    });

    return result;
  }

  private static bool IsCommandInProgress(Document doc)
  {
    try
    {
      return !string.IsNullOrEmpty(doc.CommandInProgress);
    }
    catch
    {
      return false;
    }
  }

  private static async Task<object?> BeginSessionAsync()
  {
    string? sessionId = null;

    await App.DocumentManager.ExecuteInCommandContextAsync(async _ =>
    {
      var doc = CivilExecution.RequireActiveDocument();
      var civilDoc = CivilExecution.RequireCivilDocument();
      sessionId = SessionManager.Begin(doc, civilDoc, doc.Database);
      await Task.CompletedTask;
    }, null);

    return new
    {
      sessionId,
      activeSessions = SessionManager.ActiveCount,
      message = "Session started. Use sessionExecute for multi-step scripts, then sessionCommit or sessionAbort.",
    };
  }

  private static async Task<object?> SessionExecuteAsync(
    JsonObject? parameters,
    CancellationToken cancellationToken)
  {
    var sessionId = PluginRuntime.GetRequiredString(parameters, "sessionId");
    var code = PluginRuntime.GetRequiredString(parameters, "code");
    var description = PluginRuntime.GetOptionalString(parameters, "description") ?? "Session script";
    var timeoutMs = PluginRuntime.GetOperationTimeoutMs("sessionExecute", parameters);
    var timeout = TimeSpan.FromMilliseconds(timeoutMs);
    var confirmed = parameters?["confirmed"]?.GetValue<bool>() ?? false;

    Debug.WriteLine($"[C3D-MCP] SESSION: {description}");

    object? raw = null;

    await App.DocumentManager.ExecuteInCommandContextAsync(async _ =>
    {
      var session = SessionManager.GetRequired(sessionId);
      var context = new ScriptContext(session.Document, session.CivilDoc, session.Database, session.Transaction);
      var task = RoslynExecutor.ExecuteAsync(code, context, cancellationToken, timeout, confirmed, readOnly: false);
      task.Wait(cancellationToken);
      raw = task.Result;
      await Task.CompletedTask;
    }, null);

    return ResultSerializer.Serialize(raw);
  }

  private static async Task<object?> SessionCommitAsync(JsonObject? parameters)
  {
    var sessionId = PluginRuntime.GetRequiredString(parameters, "sessionId");
    Document? doc = null;

    await App.DocumentManager.ExecuteInCommandContextAsync(async _ =>
    {
      var session = SessionManager.Remove(sessionId);
      doc = session.Document;
      session.Transaction.Commit();
      session.Dispose();
      await Task.CompletedTask;
    }, null);

    if (doc != null)
    {
      CivilExecution.RefreshDrawing(doc);
    }

    return new
    {
      sessionId,
      committed = true,
      activeSessions = SessionManager.ActiveCount,
    };
  }

  private static async Task<object?> SessionAbortAsync(JsonObject? parameters)
  {
    var sessionId = PluginRuntime.GetRequiredString(parameters, "sessionId");

    await App.DocumentManager.ExecuteInCommandContextAsync(async _ =>
    {
      var session = SessionManager.Remove(sessionId);
      session.Dispose();
      await Task.CompletedTask;
    }, null);

    return new
    {
      sessionId,
      committed = false,
      activeSessions = SessionManager.ActiveCount,
    };
  }

  private static Task<object?> DiscoverDrawingAsync(JsonObject? parameters)
  {
    var limit = parameters?["limit"]?.GetValue<int>() ?? 100;
    string[]? categories = null;

    if (parameters?["categories"] is JsonArray categoryArray)
    {
      categories = categoryArray
        .Select(node => node?.GetValue<string>())
        .Where(s => !string.IsNullOrWhiteSpace(s))
        .Select(s => s!)
        .ToArray();
    }

    return CivilExecution.ReadAsync<object?>((doc, civilDoc, db, tr) =>
      DrawingDiscovery.Run(civilDoc, tr, categories, limit));
  }

  private static Task<object?> GetHealthAsync()
  {
    return CivilExecution.ReadAsync<object?>((doc, civilDoc, db, tr) =>
    {
      var status = PluginRuntime.GetStatus();
      var operation = PluginRuntime.GetOperationStatus();
      var version = App.Version;

      return new
      {
        connected = true,
        plugin = new
        {
          isRunning = status.IsRunning,
          port = PluginRuntime.Port,
          queueDepth = status.QueueDepth,
          operationInProgress = status.OperationInProgress,
          currentOperation = status.CurrentOperation,
          activeSessions = SessionManager.ActiveCount,
        },
        operation = new
        {
          isIdle = operation.IsIdle,
          method = operation.Method,
          description = operation.Description,
          startedAtUtc = operation.StartedAtUtc,
          elapsedMs = operation.ElapsedMs,
        },
        civil3d = new
        {
          version = $"{version.Major}.{version.Minor}.{version.Revision}",
          product = HostApplicationServices.Current.Product.ToString(),
        },
        drawing = new
        {
          name = doc.Name,
          path = string.IsNullOrWhiteSpace(db.Filename) ? null : db.Filename,
          isSaved = !string.IsNullOrWhiteSpace(db.Filename),
        },
        mode = "code_execution",
        roslyn = true,
      };
    });
  }

  private static object? GetOperationStatus()
  {
    var pluginStatus = PluginRuntime.GetStatus();
    var operation = PluginRuntime.GetOperationStatus();

    return new
    {
      plugin = new
      {
        isRunning = pluginStatus.IsRunning,
        queueDepth = pluginStatus.QueueDepth,
        activeSessions = SessionManager.ActiveCount,
      },
      operation = new
      {
        isIdle = operation.IsIdle,
        method = operation.Method,
        description = operation.Description,
        startedAtUtc = operation.StartedAtUtc,
        elapsedMs = operation.ElapsedMs,
      },
    };
  }

  private static object? GetAuditLog(JsonObject? parameters)
  {
    var limit = parameters?["limit"]?.GetValue<int>() ?? 50;
    return AuditLogger.GetRecentEntries(limit);
  }

  private static object? GetSecurityPolicy() => SecurityPolicy.Describe();
}
