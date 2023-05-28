using System.Data;
using System.Data.Common;
using Oracle.DataAccess.Client;

namespace SqlFlow.Database.Oracle;

public class DatabaseOracle : IDatabase
{
    private readonly string _connectionString;

    public DatabaseOracle(string connectionString)
    {
        _connectionString = connectionString;
    }

    public IDatabase GetObjectForDatabaseNamed(string name) => throw new NotImplementedException();

    public DbExecutionResult ExecuteCommand(string query, QueryOptions? options = null)
    {
        options ??= new QueryOptions();

        using var connection = GetConnection();
        using var transaction = ConditionallyOpenTransaction(connection, options.IsTransactional);

        var queries = OracleScriptParser.ParseScript(query);
        ParsedSubQuery? mostRecentQuery = null;

        try
        {
            if (options.IsTestRun)
                new OracleCommand("SET PARSEONLY ON", connection).ExecuteNonQuery();

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
                catch (OracleException sqlException)
                {
                    return TryRollbackTransaction(transaction, mostRecentQuery.LineNumber, new DbExecutionResult(false,
                        "Error executing query",
                        queryItem.LineNumber + sqlException.Number, sqlException));
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
                new OracleCommand("SET PARSEONLY OfF", connection).ExecuteNonQuery();
        }
    }

    private OracleConnection GetConnection()
    {
        return new OracleConnection(_connectionString);
    }

    private OracleTransaction? ConditionallyOpenTransaction(OracleConnection connection, bool isTransactional)
    {
        OracleTransaction? transaction = null;
        if (isTransactional)
        {
            transaction = connection.BeginTransaction();
        }

        return transaction;
    }

    private OracleCommand SetUpCommand(OracleConnection connection, string query, int timeout)
    {
        OracleCommand command = connection.CreateCommand();
        command.CommandText = query;
        command.CommandTimeout = timeout;
        command.CommandType = CommandType.Text;
        return command;
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

    public DbExecutionResult<DbDataReader> ExecuteQueryDataReader(string query, int? timeout = null) =>
        throw new NotImplementedException();

    public DbExecutionResult<DataTable> ExecuteQueryDataTable(string query, int? timeout = null) =>
        throw new NotImplementedException();

    public DbExecutionResult<object> ExecuteQueryScalar(string query, int? timeout = null) =>
        throw new NotImplementedException();
}
