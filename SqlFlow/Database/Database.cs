using System.Data;
using System.Data.Common;

namespace SqlFlow.Database;

public abstract class Database : IDatabase
{
    protected abstract DbConnection GetConnection();
    protected abstract DbDataAdapter GetDataAdapter(DbCommand command);

    public abstract IDatabase GetObjectForDatabaseNamed(string name);

    public abstract DbExecutionResult ExecuteCommand(string query, QueryOptions? options = null);

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
            using var adapter = GetDataAdapter(command);
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

    protected DbTransaction? ConditionallyOpenTransaction(DbConnection connection, bool isTransactional)
    {
        DbTransaction? transaction = null;
        if (isTransactional)
        {
            transaction = connection.BeginTransaction();
        }

        return transaction;
    }

    protected DbCommand SetUpCommand(DbConnection connection, string query, int timeout)
    {
        DbCommand command = connection.CreateCommand();
        command.CommandText = query;
        command.CommandTimeout = timeout;
        command.CommandType = CommandType.Text;
        return command;
    }
}
