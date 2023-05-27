using System.ComponentModel;

namespace SqlFlow;

public class QueryOptions
{
    const int DefaultTimeout = 6000;
    public bool IsTransactional { get; set; } = true;
    public int Timeout { get; set; } = DefaultTimeout;
    public BackgroundWorker? Worker { get; set; } = null;
}
