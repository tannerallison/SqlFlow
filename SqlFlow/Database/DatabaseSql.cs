using System;
using System.Data;
using System.Data.Common;
using Microsoft.Data.SqlClient;

namespace SqlFlow;

public class DatabaseSql : IDatabase
{
    private readonly string _connectionString;

    public DatabaseSql(string connectionString)
    {
        _connectionString = connectionString;
    }

    public IDatabase GetObjectForDatabaseNamed(string name)
    {
        var builder = new SqlConnectionStringBuilder(_connectionString);
        builder.InitialCatalog = name;
        return new DatabaseSql(builder.ToString());
    }

    public RunResult ExecuteCommand(string query, QueryOptions? options = null)
    {
        options ??= new QueryOptions();

        using var connection = GetConnection();
        using var transaction = ConditionallyOpenTransaction(connection, options.IsTransactional);

        var queries = SqlScriptParser.ParseScript(query);
        ParsedSubQuery? mostRecentQuery = null;

        try
        {
            if (options.IsTestRun)
                new SqlCommand("SET PARSEONLY ON", connection).ExecuteNonQuery();

            foreach (ParsedSubQuery queryItem in queries)
            {
                mostRecentQuery = queryItem;

                if (options.Worker?.CancellationPending ?? false)
                    break;

                using var command = SetUpCommand(connection, queryItem.Query, options.Timeout);
                try
                {
                    command.ExecuteNonQuery();
                    if (options.IsTransactional && transaction?.Connection == null)
                        return new RunResult(false,
                            $"Query was rolled back on in the query that starts on line {queryItem.LineNumber}\r\n",
                            queryItem.LineNumber);
                }
                catch (SqlException sqlException)
                {
                    return TryRollbackTransaction(transaction, mostRecentQuery.LineNumber, new RunResult(false,
                        "Error executing query",
                        queryItem.LineNumber + sqlException.LineNumber, sqlException));
                }
            }

            if (options.Worker?.CancellationPending ?? false)
            {
                return TryRollbackTransaction(transaction, mostRecentQuery?.LineNumber,
                    new RunResult(false, "Query cancelled", mostRecentQuery?.LineNumber));
            }

            if (options.IsTransactional && transaction?.Connection == null)
                return new RunResult(false, "Query was rolled back.");

            transaction?.Commit();

            return new RunResult(true, "Query executed successfully");
        }
        finally
        {
            if (options.IsTestRun)
                new SqlCommand("SET PARSEONLY OfF", connection).ExecuteNonQuery();
        }
    }

    public RunResult<DbDataReader> ExecuteQueryDataReader(string query, int? timeout = null)
    {
        timeout ??= new QueryOptions().Timeout;
        using var connection = GetConnection();
        try
        {
            using var command = SetUpCommand(connection, query, timeout.Value);
            var output = command.ExecuteReader();
            return new RunResult<DbDataReader>(output, true, "Query executed successfully");
        }
        catch (Exception ex)
        {
            return new RunResult<DbDataReader>(null, false, "Error executing query", exception: ex);
        }
    }

    public RunResult<DataTable> ExecuteQueryDataTable(string query, int? timeout = null)
    {
        timeout ??= new QueryOptions().Timeout;
        var data = new DataTable();
        using var connection = GetConnection();
        try
        {
            using var command = SetUpCommand(connection, query, timeout.Value);
            using var adapter = new SqlDataAdapter(command);
            adapter.Fill(data);
            return new RunResult<DataTable>(data, true, "Query executed successfully");
        }
        catch (Exception ex)
        {
            return new RunResult<DataTable>(null, false, "Error executing query", exception: ex);
        }
    }

    public RunResult<object> ExecuteQueryScalar(string query, int? timeout = null)
    {
        timeout ??= new QueryOptions().Timeout;
        using var connection = GetConnection();
        try
        {
            using var command = SetUpCommand(connection, query, timeout.Value);
            var output = command.ExecuteScalar();
            return new RunResult<object>(output, true, "Query executed successfully");
        }
        catch (Exception ex)
        {
            return new RunResult<object>(null, false, "Error executing query", exception: ex);
        }
    }

    private SqlCommand SetUpCommand(SqlConnection connection, string query, int timeout)
    {
        SqlCommand command = connection.CreateCommand();
        command.CommandText = query;
        command.CommandTimeout = timeout;
        command.CommandType = CommandType.Text;
        return command;
    }

    private SqlTransaction? ConditionallyOpenTransaction(SqlConnection connection, bool isTransactional)
    {
        SqlTransaction? transaction = null;
        if (isTransactional)
        {
            transaction = connection.BeginTransaction();
        }

        return transaction;
    }

    private SqlConnection GetConnection()
    {
        return new SqlConnection(_connectionString);
    }

    private static RunResult TryRollbackTransaction(DbTransaction? transaction, int? lineNumber,
        RunResult innerResult)
    {
        try
        {
            transaction?.Rollback();
        }
        catch (Exception ex)
        {
            return new RunResult(false, "Unable to roll back transaction", lineNumber, ex, innerResult: innerResult);
        }

        return innerResult;
    }
}
