using System.Diagnostics;
using Serilog;
using SqlFlow.Database;

namespace SqlFlow;

public record RunOptions
{
    public IDatabase Database { get; set; }
    public bool TestRun { get; set; }
    public bool BreakOnError { get; init; }
    public IProgress<RunProgress>? Progress { get; set; }
    public CancellationToken? CancellationToken { get; set; }
}

public class RunResult
{
    public List<DbExecutionResult> Results { get; set; } = new();
    public bool Success => Results.All(r => r.Success);
    public bool Cancelled { get; set; } = false;
}

public struct RunProgress
{
    public RunProgress(int percent, string message)
    {
        Percent = percent;
        Message = message;
    }

    public int Percent { get; set; }
    public string Message { get; set; }
}

public class RunPackage
{
    private readonly RunOptions _options;
    private readonly IEnumerable<Script> _scripts;
    private readonly ICollection<Variable> _variables;
    private readonly ILogger _logger;

    public RunPackage(IEnumerable<Script> scripts, ICollection<Variable> variables, RunOptions options,
        ILogger? logger = null)
    {
        _scripts = scripts;
        _variables = variables;
        _options = options;
        _logger = logger ?? new LoggerConfiguration().CreateLogger();
    }

    private void Report(int progress, string message)
    {
        _options.Progress?.Report(new RunProgress(progress, message));
    }

    public RunResult Run()
    {
        decimal totalCount = _scripts.Count();
        decimal completeCount = 0;

        IDatabase database = _options.Database;
        var runResult = new RunResult();

        foreach (var script in _scripts)
        {
            var progress = (int)(++completeCount / totalCount * 100);

            if (_options.CancellationToken?.IsCancellationRequested == true)
            {
                _logger.Information("Cancelled prior to {Script}", script.ScriptName);
                runResult.Cancelled = true;
                return runResult;
            }

            var query = script.GetReplacedText(_variables);
            var options = new QueryOptions(script.Timeout, script.IsTransactional, _options.CancellationToken,
                _options.TestRun);

            var scriptDatabase = GetIDatabaseToUse(database, script);

            Report(progress, $"Beginning {script.ScriptName}...");
            _logger.Information("Running {Script}", script.ScriptName);

            var stopwatch = Stopwatch.StartNew();
            var dbExecutionResult = scriptDatabase.ExecuteCommand(query, options);
            stopwatch.Stop();

            runResult.Results.Add(dbExecutionResult);

            if (dbExecutionResult.Success)
            {
                Report(progress, $"Completed {script.ScriptName}");
                _logger.Information("Completed {Script} in {Elapsed:000} ms", script.ScriptName,
                    stopwatch.ElapsedMilliseconds);
            }
            else
            {
                Report(progress,
                    $"Failed {script.ScriptName} on line {dbExecutionResult.LineNumber}: {dbExecutionResult.Message}");
                _logger.Error("Failed {Script} in {Elapsed:000} ms", script.ScriptName,
                    stopwatch.ElapsedMilliseconds);

                if (_options.BreakOnError)
                    break;
            }
        }

        return runResult;
    }

    private IDatabase GetIDatabaseToUse(IDatabase optionsDatabase, Script script)
    {
        if (!script.SpecifiesDatabase) return optionsDatabase;

        var dbName = script.GetDatabaseToUse(_variables)!;
        return optionsDatabase.GetObjectForDatabaseNamed(dbName);
    }
}
