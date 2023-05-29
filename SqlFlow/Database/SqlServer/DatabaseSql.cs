using System.Data.Common;
using Microsoft.Data.SqlClient;

namespace SqlFlow.Database.SqlServer;

public class DatabaseSql : Database
{
    private readonly string _connectionString;

    public DatabaseSql(string connectionString)
    {
        _connectionString = connectionString;
    }

    protected override DbDataAdapter GetDataAdapter(DbCommand command) => new SqlDataAdapter(command as SqlCommand);

    public override IDatabase GetObjectForDatabaseNamed(string name)
    {
        var builder = new SqlConnectionStringBuilder(_connectionString);
        builder.InitialCatalog = name;
        return new DatabaseSql(builder.ToString());
    }

    public override DbExecutionResult ExecuteCommand(string query, QueryOptions? options = null)
    {
        options ??= new QueryOptions();

        using var connection = GetConnection();
        using var transaction = ConditionallyOpenTransaction(connection, options.IsTransactional);

        var queries = SqlScriptParser.ParseScript(query);
        ParsedSubQuery? mostRecentQuery = null;

        try
        {
            if (options.IsTestRun)
                SetUpCommand(connection, "SET PARSEONLY ON", options.Timeout).ExecuteNonQuery();

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
                SetUpCommand(connection, "SET PARSEONLY OFF", options.Timeout).ExecuteNonQuery();
        }
    }

    protected override DbConnection GetConnection() => new SqlConnection(_connectionString);

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
