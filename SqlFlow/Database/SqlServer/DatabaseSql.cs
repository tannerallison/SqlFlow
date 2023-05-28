using System.Data;
using System.Data.Common;
using Microsoft.Data.SqlClient;

namespace SqlFlow.Database.SqlServer;

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

    public DbExecutionResult ExecuteCommand(string query, QueryOptions? options = null)
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

                if (options.CancellationToken?.IsCancellationRequested ?? false)
                    break;

                using var command = SetUpCommand(connection, queryItem.Query, options.Timeout);
                try
                {
                    command.ExecuteNonQuery();
                    if (options.IsTransactional && transaction?.Connection == null)
                        return new DbExecutionResult(false,
                            $"Query was rolled back on in the query that starts on line {queryItem.LineNumber}\r\n",
                            queryItem.LineNumber);
                }
                catch (SqlException sqlException)
                {
                    return TryRollbackTransaction(transaction, mostRecentQuery.LineNumber, new DbExecutionResult(false,
                        "Error executing query",
                        queryItem.LineNumber + sqlException.LineNumber, sqlException));
                }
            }

            if (options.CancellationToken?.IsCancellationRequested ?? false)
            {
                return TryRollbackTransaction(transaction, mostRecentQuery?.LineNumber,
                    new DbExecutionResult(false, "Query cancelled", mostRecentQuery?.LineNumber));
            }

            if (options.IsTransactional && transaction?.Connection == null)
                return new DbExecutionResult(false, "Query was rolled back.");

            transaction?.Commit();

            return new DbExecutionResult(true, "Query executed successfully");
        }
        finally
        {
            if (options.IsTestRun)
                new SqlCommand("SET PARSEONLY OfF", connection).ExecuteNonQuery();
        }
    }

    public DbExecutionResult<DbDataReader> ExecuteQueryDataReader(string query, int? timeout = null)
    {
        timeout ??= new QueryOptions().Timeout;
        using var connection = GetConnection();
        try
        {
            using var command = SetUpCommand(connection, query, timeout.Value);
            var output = command.ExecuteReader();
            return new DbExecutionResult<DbDataReader>(output, true, "Query executed successfully");
        }
        catch (Exception ex)
        {
            return new DbExecutionResult<DbDataReader>(null, false, "Error executing query", exception: ex);
        }
    }

    public DbExecutionResult<DataTable> ExecuteQueryDataTable(string query, int? timeout = null)
    {
        timeout ??= new QueryOptions().Timeout;
        var data = new DataTable();
        using var connection = GetConnection();
        try
        {
            using var command = SetUpCommand(connection, query, timeout.Value);
            using var adapter = new SqlDataAdapter(command);
            adapter.Fill(data);
            return new DbExecutionResult<DataTable>(data, true, "Query executed successfully");
        }
        catch (Exception ex)
        {
            return new DbExecutionResult<DataTable>(null, false, "Error executing query", exception: ex);
        }
    }

    public DbExecutionResult<object> ExecuteQueryScalar(string query, int? timeout = null)
    {
        timeout ??= new QueryOptions().Timeout;
        using var connection = GetConnection();
        try
        {
            using var command = SetUpCommand(connection, query, timeout.Value);
            var output = command.ExecuteScalar();
            return new DbExecutionResult<object>(output, true, "Query executed successfully");
        }
        catch (Exception ex)
        {
            return new DbExecutionResult<object>(null, false, "Error executing query", exception: ex);
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

    private static DbExecutionResult TryRollbackTransaction(DbTransaction? transaction, int? lineNumber,
        DbExecutionResult innerResult)
    {
        try
        {
            transaction?.Rollback();
        }
        catch (Exception ex)
        {
            return new DbExecutionResult(false, "Unable to roll back transaction", lineNumber, ex, innerResult: innerResult);
        }

        return innerResult;
    }
}
