using System.Diagnostics;
using Serilog;
using SqlFlow.Database;
using SqlFlow.Manager;

namespace SqlFlow.Runner;

public class RunPackage
{
    private readonly IDatabase _database;
    private readonly IEnumerable<Script> _scripts;
    private readonly ICollection<Variable> _variables;
    private readonly RunOptions _options;
    private readonly ILogger? _logger;

    public RunPackage(IEnumerable<Script> scripts, ICollection<Variable> variables, IDatabase database,
        RunOptions? options = null)
    {
        _scripts = scripts;
        _variables = variables;
        _database = database;
        _options = options ?? new RunOptions();
        _logger = _options.Logger;
    }

    public RunResult Run()
    {
        decimal totalCount = _scripts.Count();
        decimal completeCount = 0;

        var runResult = new RunResult();
        var stopwatch = Stopwatch.StartNew();

        foreach (var script in _scripts)
        {
            var progress = (int)(++completeCount / totalCount * 100);

            if (_options.CancellationToken?.IsCancellationRequested == true)
            {
                _logger?.Information("Cancelled prior to {Script}", script.ScriptName);
                runResult.Cancelled = true;
                return runResult;
            }

            var query = script.GetReplacedText(_variables);
            var options = new QueryOptions(script.Timeout, script.IsTransactional, _options.CancellationToken,
                _options.TestRun);

            var scriptDatabase = GetIDatabaseToUse(script);

            Report(progress, $"Executing {script.ScriptName}...");
            _logger?.Information("Executing {Script}", script.ScriptName);

            stopwatch.Restart();
            var dbExecutionResult = scriptDatabase.ExecuteCommand(query, options);
            stopwatch.Stop();

            runResult.Results.Add(dbExecutionResult);

            if (dbExecutionResult.Success)
            {
                Report(progress, $"Completed {script.ScriptName}");
                _logger?.Information("Completed {Script} in {Elapsed:000} ms", script.ScriptName,
                    stopwatch.ElapsedMilliseconds);
            }
            else
            {
                Report(progress,
                    $"Failed {script.ScriptName} on line {dbExecutionResult.LineNumber}: {dbExecutionResult.Message}");
                _logger?.Warning("Failed {Script} in {Elapsed:000} ms", script.ScriptName,
                    stopwatch.ElapsedMilliseconds);
            }

            if (!dbExecutionResult.Success && _options.BreakOnError)
                break;
        }

        return runResult;
    }

    private void Report(int progress, string message) => _options.Progress?.Report(new RunProgress(progress, message));

    private IDatabase GetIDatabaseToUse(Script script)
    {
        if (!script.SpecifiesDatabase) return _database;

        var dbName = script.GetDatabaseToUse(_variables)!;
        return _database.GetObjectForDatabaseNamed(dbName);
    }
}
