namespace SqlFlow;

public class QueryOptions
{
    public QueryOptions(int? timeout = null, bool? isTransactional = true, CancellationToken? cancellationToken = null,
        bool isTestRun = false)
    {
        Timeout = timeout ?? DefaultTimeout;
        IsTransactional = isTransactional ?? true;
        CancellationToken = cancellationToken;
        IsTestRun = isTestRun;
    }

    const int DefaultTimeout = 6000;
    public bool IsTransactional { get; }

    public int Timeout { get; }

    public bool IsTestRun { get; }

    public CancellationToken? CancellationToken { get; }
}
