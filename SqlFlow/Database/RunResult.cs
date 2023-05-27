using System;

namespace SqlFlow;

public class RunResult<T> : RunResult
{
    public RunResult(T? output, bool success, string message, int? lineNumber = null, Exception? exception = null,
        RunResult? innerResult = null) : base(success, message, lineNumber, exception, innerResult)
    {
        Output = output;
    }

    public T? Output { get; set; }
}

public class RunResult : EventArgs
{
    public RunResult(bool success, string message, int? lineNumber = null, Exception? exception = null,
        RunResult? innerResult = null)
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
    public RunResult? InnerResult { get; set; }
}
