using System.ComponentModel;

namespace SqlFlow;

public class QueryOptions
{
    public QueryOptions(int? timeout = null, bool? isTransactional = true, BackgroundWorker? worker = null,
        bool isTestRun = false)
    {
        Timeout = timeout ?? DefaultTimeout;
        IsTransactional = isTransactional ?? true;
        Worker = worker;
        IsTestRun = isTestRun;
    }

    const int DefaultTimeout = 6000;
    public bool IsTransactional { get; }

    public int Timeout { get; }

    public bool IsTestRun { get; }

    public BackgroundWorker? Worker { get; }
}
