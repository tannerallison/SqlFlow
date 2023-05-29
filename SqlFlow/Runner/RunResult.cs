using SqlFlow.Database;

namespace SqlFlow.Runner;

public class RunResult
{
    public List<DbExecutionResult> Results { get; set; } = new();
    public bool Success => Results.All(r => r.Success);
    public bool Cancelled { get; set; }
}
