using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.Civil.ApplicationServices;
using App = Autodesk.AutoCAD.ApplicationServices.Application;

namespace Civil3DMcpPlugin;

/// <summary>
/// Helper for executing Civil 3D API operations safely on the main thread.
/// The Civil 3D API is single-threaded — all operations must be marshaled to
/// the main AutoCAD thread via ExecuteInCommandContextAsync.
/// </summary>
public static class CivilExecution
{
  /// <summary>
  /// Execute an operation within a proper document lock and transaction.
  /// If <paramref name="write"/> is true, the transaction is committed.
  /// </summary>
  public static async Task<T> ExecuteAsync<T>(
    Func<Document, CivilDocument, Database, Transaction, T> action,
    bool write)
  {
    T? result = default;
    Exception? capturedException = null;

    await App.DocumentManager.ExecuteInCommandContextAsync(async _ =>
    {
      try
      {
        var doc = App.DocumentManager.MdiActiveDocument
          ?? throw new JsonRpcDispatchException("CIVIL3D.NO_DRAWING", "No active drawing is open in Civil 3D.");
        var civilDoc = CivilApplication.ActiveDocument
          ?? throw new JsonRpcDispatchException("CIVIL3D.NO_DRAWING", "No active Civil 3D document is available.");
        var database = doc.Database;

        using var documentLock = doc.LockDocument();
        using var transaction = database.TransactionManager.StartTransaction();

        result = action(doc, civilDoc, database, transaction);

        if (write)
        {
          transaction.Commit();
        }
      }
      catch (Exception ex)
      {
        capturedException = ex;
      }

      await Task.CompletedTask;
    }, null);

    if (capturedException != null)
    {
      throw capturedException;
    }

    return result!;
  }

  /// <summary>Execute a read-only operation (no commit).</summary>
  public static Task<T> ReadAsync<T>(
    Func<Document, CivilDocument, Database, Transaction, T> action)
    => ExecuteAsync(action, false);

  /// <summary>Execute a write operation (commits the transaction).</summary>
  public static Task<T> WriteAsync<T>(
    Func<Document, CivilDocument, Database, Transaction, T> action)
    => ExecuteAsync(action, true);
}
