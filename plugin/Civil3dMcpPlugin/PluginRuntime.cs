using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Civil3DMcpPlugin;

/// <summary>Status record for plugin diagnostics.</summary>
public sealed record PluginStatus(
  bool IsRunning,
  bool OperationInProgress,
  string? CurrentOperation,
  int QueueDepth
);

/// <summary>
/// Exception type for JSON-RPC dispatch errors, carrying an error code.
/// </summary>
public sealed class JsonRpcDispatchException : Exception
{
  public JsonRpcDispatchException(string code, string message, object? errorData = null) : base(message)
  {
    Code = code;
    ErrorData = errorData;
  }

  public string Code { get; }
  public object? ErrorData { get; }
}

/// <summary>
/// Manages the plugin lifecycle: starts/stops the TCP server,
/// handles raw JSON-RPC requests, and provides parameter extraction helpers.
/// </summary>
public static class PluginRuntime
{
  public const int DefaultPort = 8080;

  /// <summary>TCP port read from CIVIL3D_PORT env var, default 8080.</summary>
  public static int Port
  {
    get
    {
      var env = Environment.GetEnvironmentVariable("CIVIL3D_PORT");
      return int.TryParse(env, out var port) && port is > 0 and <= 65535
        ? port
        : DefaultPort;
    }
  }

  private static readonly object Sync = new();
  private static readonly SemaphoreSlim RequestGate = new(1, 1);
  private static RpcTcpServer? _server;
  private static int _queueDepth;
  private static int _activeOperations;
  private static string? _currentOperation;

  public static void StartServer()
  {
    lock (Sync)
    {
      if (_server != null) return;
      _server = new RpcTcpServer(Port, HandleRawRequestAsync);
      _server.Start();
    }
  }

  public static void StopServer()
  {
    lock (Sync)
    {
      _server?.Stop();
      _server = null;
      _currentOperation = null;
      _activeOperations = 0;
      _queueDepth = 0;
    }
  }

  public static PluginStatus GetStatus()
  {
    lock (Sync)
    {
      return new PluginStatus(
        _server != null,
        _activeOperations > 0,
        _currentOperation,
        _queueDepth
      );
    }
  }

  /// <summary>
  /// Parses a raw JSON-RPC request string, dispatches to the appropriate
  /// command handler, and returns the serialized JSON-RPC response.
  /// </summary>
  public static async Task<string> HandleRawRequestAsync(
    string rawRequest,
    CancellationToken cancellationToken)
  {
    JsonNode? parsed;
    try
    {
      parsed = JsonNode.Parse(rawRequest);
    }
    catch (Exception ex)
    {
      return SerializeError(null, "CIVIL3D.INVALID_INPUT", $"Invalid JSON request: {ex.Message}");
    }

    if (parsed is not JsonObject request)
    {
      return SerializeError(null, "CIVIL3D.INVALID_INPUT", "JSON-RPC request must be an object.");
    }

    var id = request["id"]?.DeepClone();
    var method = request["method"]?.GetValue<string>();
    var parameters = request["params"] as JsonObject;

    if (string.IsNullOrWhiteSpace(method))
    {
      return SerializeError(id, "CIVIL3D.INVALID_INPUT", "JSON-RPC request is missing method.");
    }

    lock (Sync) { _queueDepth++; }

    await RequestGate.WaitAsync(cancellationToken);

    var description = GetOptionalString(parameters, "description");
    var timeoutMs = GetOperationTimeoutMs(method, parameters);
    var sw = Stopwatch.StartNew();

    using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
    timeoutCts.CancelAfter(timeoutMs);

    try
    {
      lock (Sync)
      {
        _queueDepth--;
        _activeOperations++;
        _currentOperation = method;
      }

      OperationTracker.Begin(method, description);

      var result = await CommandDispatcher.DispatchAsync(method, parameters, timeoutCts.Token);
      sw.Stop();
      AuditLogger.LogSuccess(method, description, sw.ElapsedMilliseconds);
      return SerializeResult(id, result);
    }
    catch (JsonRpcDispatchException ex)
    {
      sw.Stop();
      AuditLogger.LogFailure(method, description, sw.ElapsedMilliseconds, ex.Code, ex.Message);
      return SerializeError(id, ex.Code, ex.Message, ex.ErrorData);
    }
    catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
    {
      sw.Stop();
      var message = $"Operation timed out after {timeoutMs}ms.";
      AuditLogger.LogFailure(method, description, sw.ElapsedMilliseconds, "CIVIL3D.TIMEOUT", message);
      return SerializeError(id, "CIVIL3D.TIMEOUT", message, new { timeoutMs, method });
    }
    catch (Exception ex)
    {
      sw.Stop();
      AuditLogger.LogFailure(method, description, sw.ElapsedMilliseconds, "CIVIL3D.TRANSACTION_FAILED", ex.Message);
      return SerializeError(
        id,
        "CIVIL3D.TRANSACTION_FAILED",
        ex.Message,
        new { type = ex.GetType().Name, stackTrace = ex.StackTrace }
      );
    }
    finally
    {
      OperationTracker.End();

      lock (Sync)
      {
        _activeOperations = Math.Max(0, _activeOperations - 1);
        _currentOperation = _activeOperations == 0 ? null : _currentOperation;
      }

      RequestGate.Release();
    }
  }

  /// <summary>Resolve operation timeout from params, method defaults, or environment.</summary>
  public static int GetOperationTimeoutMs(string method, JsonObject? parameters)
  {
    var fromParams = GetOptionalInt(parameters, "timeoutMs");
    if (fromParams is > 0) return fromParams.Value;

    var envKey = method switch
    {
      "executeCode" => "CIVIL3D_EXECUTE_TIMEOUT_MS",
      "executeNativeCommand" => "CIVIL3D_COMMAND_TIMEOUT_MS",
      "discoverDrawing" => "CIVIL3D_DISCOVER_TIMEOUT_MS",
      "sessionExecute" => "CIVIL3D_EXECUTE_TIMEOUT_MS",
      _ => "CIVIL3D_DEFAULT_TIMEOUT_MS",
    };

    var env = Environment.GetEnvironmentVariable(envKey)
      ?? Environment.GetEnvironmentVariable("CIVIL3D_DEFAULT_TIMEOUT_MS");

    if (int.TryParse(env, out var ms) && ms > 0)
      return ms;

    return method switch
    {
      "discoverDrawing" => 60_000,
      "executeNativeCommand" => 120_000,
      _ => 120_000,
    };
  }

  public static OperationStatus GetOperationStatus()
  {
    int queueDepth;
    lock (Sync) { queueDepth = _queueDepth; }
    return OperationTracker.GetStatus(queueDepth, SessionManager.ActiveCount);
  }

  // ── Parameter extraction helpers ──

  public static string GetRequiredString(JsonObject? parameters, string name)
  {
    var value = parameters?[name];
    if (value == null)
      throw new JsonRpcDispatchException("CIVIL3D.INVALID_INPUT", $"Missing required parameter '{name}'.");
    var s = value.GetValue<string>();
    if (string.IsNullOrWhiteSpace(s))
      throw new JsonRpcDispatchException("CIVIL3D.INVALID_INPUT", $"Parameter '{name}' must be a non-empty string.");
    return s;
  }

  public static double GetRequiredDouble(JsonObject? parameters, string name)
  {
    var value = parameters?[name];
    if (value == null)
      throw new JsonRpcDispatchException("CIVIL3D.INVALID_INPUT", $"Missing required parameter '{name}'.");
    return value.GetValue<double>();
  }

  public static int GetRequiredInt(JsonObject? parameters, string name)
  {
    var value = parameters?[name];
    if (value == null)
      throw new JsonRpcDispatchException("CIVIL3D.INVALID_INPUT", $"Missing required parameter '{name}'.");
    return value.GetValue<int>();
  }

  public static string? GetOptionalString(JsonObject? parameters, string name)
    => parameters?[name]?.GetValue<string?>();

  public static double? GetOptionalDouble(JsonObject? parameters, string name)
    => parameters?[name] != null ? parameters[name]!.GetValue<double>() : null;

  public static int? GetOptionalInt(JsonObject? parameters, string name)
    => parameters?[name] != null ? parameters[name]!.GetValue<int>() : null;

  public static bool? GetOptionalBool(JsonObject? parameters, string name)
    => parameters?[name] != null ? parameters[name]!.GetValue<bool>() : null;

  // ── JSON-RPC serialization ──

  private static string SerializeResult(JsonNode? id, object? result)
  {
    var response = new JsonObject
    {
      ["jsonrpc"] = "2.0",
      ["id"] = id,
      ["result"] = result == null ? null : JsonSerializer.SerializeToNode(result, JsonSerializerOptions),
    };
    return response.ToJsonString(JsonSerializerOptions);
  }

  private static string SerializeError(JsonNode? id, string code, string message, object? data = null)
  {
    var error = new JsonObject
    {
      ["code"] = code,
      ["message"] = message,
    };

    if (data != null)
    {
      error["data"] = JsonSerializer.SerializeToNode(data, JsonSerializerOptions);
    }

    var response = new JsonObject
    {
      ["jsonrpc"] = "2.0",
      ["id"] = id,
      ["error"] = error,
    };
    return response.ToJsonString(JsonSerializerOptions);
  }

  private static readonly JsonSerializerOptions JsonSerializerOptions = new()
  {
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    WriteIndented = false,
  };
}
