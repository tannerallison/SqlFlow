using System.Data;
using System.Data.Common;
using Oracle.DataAccess.Client;

namespace SqlFlow;

public class DatabaseOracle : IDatabase
{
    private readonly string _connectionString;

    public DatabaseOracle(string connectionString)
    {
        _connectionString = connectionString;
    }

    public IDatabase GetObjectForDatabaseNamed(string name) => throw new NotImplementedException();

    public RunResult ExecuteCommand(string query, QueryOptions? options = null)
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
                catch (OracleException sqlException)
                {
                    return TryRollbackTransaction(transaction, mostRecentQuery.LineNumber, new RunResult(false,
                        "Error executing query",
                        queryItem.LineNumber + sqlException.Number, sqlException));
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

    public RunResult<DbDataReader> ExecuteQueryDataReader(string query, int? timeout = null) =>
        throw new NotImplementedException();

    public RunResult<DataTable> ExecuteQueryDataTable(string query, int? timeout = null) =>
        throw new NotImplementedException();

    public RunResult<object> ExecuteQueryScalar(string query, int? timeout = null) =>
        throw new NotImplementedException();
}
