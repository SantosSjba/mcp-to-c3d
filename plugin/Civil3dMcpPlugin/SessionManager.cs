using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.Civil.ApplicationServices;

namespace Civil3DMcpPlugin;

/// <summary>
/// Holds an open document lock and transaction across multiple RPC calls.
/// </summary>
public sealed class ScriptSession : IDisposable
{
  public string Id { get; }
  public Document Document { get; }
  public CivilDocument CivilDoc { get; }
  public Database Database { get; }
  public Transaction Transaction { get; }
  public DocumentLock DocumentLock { get; }
  public DateTime CreatedAt { get; } = DateTime.UtcNow;

  internal ScriptSession(
    string id,
    Document document,
    CivilDocument civilDoc,
    Database database,
    DocumentLock documentLock,
    Transaction transaction)
  {
    Id = id;
    Document = document;
    CivilDoc = civilDoc;
    Database = database;
    DocumentLock = documentLock;
    Transaction = transaction;
  }

  public void Dispose()
  {
    DocumentLock.Dispose();
    Transaction.Dispose();
  }
}

/// <summary>
/// Manages multi-step script sessions with a shared transaction.
/// </summary>
public static class SessionManager
{
  private static readonly object Sync = new();
  private static readonly Dictionary<string, ScriptSession> Sessions = new();

  public static string Begin(Document document, CivilDocument civilDoc, Database database)
  {
    var id = Guid.NewGuid().ToString("N");
    var documentLock = document.LockDocument();
    var transaction = database.TransactionManager.StartTransaction();

    var session = new ScriptSession(id, document, civilDoc, database, documentLock, transaction);

    lock (Sync)
    {
      Sessions[id] = session;
    }

    return id;
  }

  public static ScriptSession GetRequired(string sessionId)
  {
    lock (Sync)
    {
      if (!Sessions.TryGetValue(sessionId, out var session))
      {
        throw new JsonRpcDispatchException(
          "CIVIL3D.SESSION_NOT_FOUND",
          $"Session '{sessionId}' not found or already closed.",
          new { sessionId }
        );
      }

      return session;
    }
  }

  public static ScriptSession Remove(string sessionId)
  {
    lock (Sync)
    {
      if (!Sessions.TryGetValue(sessionId, out var session))
      {
        throw new JsonRpcDispatchException(
          "CIVIL3D.SESSION_NOT_FOUND",
          $"Session '{sessionId}' not found or already closed.",
          new { sessionId }
        );
      }

      Sessions.Remove(sessionId);
      return session;
    }
  }

  public static int ActiveCount
  {
    get
    {
      lock (Sync) return Sessions.Count;
    }
  }
}
