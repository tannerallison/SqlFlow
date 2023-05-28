namespace SqlFlow.Database;

public class DbExecutionResult<T> : DbExecutionResult
{
    public DbExecutionResult(T? output, bool success, string message, int? lineNumber = null, Exception? exception = null,
        DbExecutionResult? innerResult = null) : base(success, message, lineNumber, exception, innerResult)
    {
        Output = output;
    }

    public T? Output { get; set; }
}

public class DbExecutionResult : EventArgs
{
    public DbExecutionResult(bool success, string message, int? lineNumber = null, Exception? exception = null,
        DbExecutionResult? innerResult = null)
    {
        Success = success;
        Message = message;
        Exception = exception;
        LineNumber = lineNumber;
        InnerResult = innerResult;
    }

    public bool Success { get; set; }
    public string Message { get; set; }
    public Exception? Exception { get; set; }
    public int? LineNumber { get; set; }
    public DbExecutionResult? InnerResult { get; set; }
}
