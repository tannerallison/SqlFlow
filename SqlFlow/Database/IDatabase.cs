using System.Data;
using System.Data.Common;

namespace SqlFlow;

public interface IDatabase
{
    /// <summary>
    /// Execute a command against the database.
    /// </summary>
    /// <param name="query"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    RunResult ExecuteCommand(string query, QueryOptions? options = null);

    /// <summary>
    /// Execute a query against the database and return a data reader.
    /// </summary>
    /// <param name="query"></param>
    /// <param name="timeout"></param>
    /// <returns></returns>
    RunResult<DbDataReader> ExecuteQueryDataReader(string query, int? timeout = null);

    /// <summary>
    /// Execute a query against the database and return a data table.
    /// </summary>
    /// <param name="query"></param>
    /// <param name="timeout"></param>
    /// <returns></returns>
    RunResult<DataTable> ExecuteQueryDataTable(string query, int? timeout = null);

    /// <summary>
    /// Execute a query against the database and return a scalar value.
    /// </summary>
    /// <param name="query"></param>
    /// <param name="timeout"></param>
    /// <returns></returns>
    RunResult<object> ExecuteQueryScalar(string query, int? timeout = null);
}
