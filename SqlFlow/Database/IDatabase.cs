using System.Data;
using System.Data.Common;

namespace SqlFlow.Database;

public interface IDatabase
{
    /// <summary>
    /// Returns a new IDatabase object for the same server with the given database name.
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public IDatabase GetObjectForDatabaseNamed(string name);

    /// <summary>
    /// Execute a command against the database.
    /// </summary>
    /// <param name="query"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    DbExecutionResult ExecuteCommand(string query, QueryOptions? options = null);

    /// <summary>
    /// Execute a query against the database and return a data reader.
    /// </summary>
    /// <param name="query"></param>
    /// <param name="timeout"></param>
    /// <returns></returns>
    DbExecutionResult<DbDataReader> ExecuteQueryDataReader(string query, int? timeout = null);

    /// <summary>
    /// Execute a query against the database and return a data table.
    /// </summary>
    /// <param name="query"></param>
    /// <param name="timeout"></param>
    /// <returns></returns>
    DbExecutionResult<DataTable> ExecuteQueryDataTable(string query, int? timeout = null);

    /// <summary>
    /// Execute a query against the database and return a scalar value.
    /// </summary>
    /// <param name="query"></param>
    /// <param name="timeout"></param>
    /// <returns></returns>
    DbExecutionResult<object> ExecuteQueryScalar(string query, int? timeout = null);
}
