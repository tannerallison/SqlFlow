using Serilog;

namespace SqlFlow.Runner;

public record RunOptions
{
    public bool TestRun { get; init; }
    public bool BreakOnError { get; init; }
    public IProgress<RunProgress>? Progress { get; init; }
    public CancellationToken? CancellationToken { get; init; }
    public ILogger? Logger { get; init; }
}
