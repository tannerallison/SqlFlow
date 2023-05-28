using System.Data.Common;
using Oracle.DataAccess.Client;

namespace SqlFlow.Database.Oracle;

public class DatabaseOracle : Database
{
    private readonly string _connectionString;

    public DatabaseOracle(string connectionString)
    {
        _connectionString = connectionString;
    }

    protected override DbDataAdapter GetDataAdapter(DbCommand command) => new OracleDataAdapter(command as OracleCommand);

    public override IDatabase GetObjectForDatabaseNamed(string name) => throw new NotImplementedException();

    public override DbExecutionResult ExecuteCommand(string query, QueryOptions? options = null)
    {
        options ??= new QueryOptions();

        if (options.IsTestRun)
            throw new NotSupportedException("Test run is not supported for Oracle databases");

        using var connection = GetConnection();
        using var transaction = ConditionallyOpenTransaction(connection, options.IsTransactional);

        var queries = OracleScriptParser.ParseScript(query);
        ParsedSubQuery? mostRecentQuery = null;

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

    protected override DbConnection GetConnection() => new OracleConnection(_connectionString);

    private static DbExecutionResult TryRollbackTransaction(DbTransaction? transaction, int? lineNumber,
        DbExecutionResult innerResult)
    {
        try
        {
            transaction?.Rollback();
        }
        catch (Exception ex)
        {
            return new DbExecutionResult(false, "Unable to roll back transaction", lineNumber, ex,
                innerResult: innerResult);
        }

        return innerResult;
    }
}
