using System.ComponentModel;
using System.Diagnostics;
using Microsoft.Data.SqlClient;
using Serilog;

namespace SqlFlow;

public class RunOptions
{
    public IDatabase Database { get; set; }
    public bool TestRun { get; set; }
    public bool BreakOnError { get; set; }
    public BackgroundWorker? Worker { get; set; }
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

    // Listener for Progress Updates
    public event ProgressChangedEventHandler ProgressChanged;

    // Listener for Completion
    public event RunWorkerCompletedEventHandler Completed;

    public BackgroundWorker Run()
    {
        var worker = SetUpWorker();
        worker.RunWorkerAsync();
        return worker;
    }


    private BackgroundWorker SetUpWorker()
    {
        var worker = _options.Worker;
        if (worker == null)
        {
            worker = new BackgroundWorker
                { WorkerSupportsCancellation = true, WorkerReportsProgress = true };
            worker.ProgressChanged += ProgressChanged;
            worker.RunWorkerCompleted += Completed;
            worker.DoWork += Run;
        }

        return worker;
    }

    private void Report(decimal progress, string message)
    {
        ProgressChanged(this,
            new ProgressChangedEventArgs((int)progress, message));
    }

    private void Run(object? sender, DoWorkEventArgs e)
    {
        var worker = (BackgroundWorker)sender!;

        decimal totalCount = _scripts.Count();
        decimal completeCount = 0;

        IDatabase database = _options.Database;

        foreach (var script in _scripts)
        {
            var progress = ++completeCount / totalCount * 100;

            if (worker.CancellationPending)
            {
                _logger.Information("Cancelled prior to {Script}", script.ScriptName);
                e.Cancel = true;
                return;
            }

            var query = script.GetReplacedText(_variables);
            var options = new QueryOptions(script.Timeout, script.IsTransactional, worker, _options.TestRun);

            var scriptDatabase = GetIDatabaseToUse(database, script);

            Report(progress, $"Beginning {script.ScriptName}...");
            _logger.Information("Running {Script}", script.ScriptName);

            var stopwatch = Stopwatch.StartNew();
            var result = database.ExecuteCommand(query, options);
            stopwatch.Stop();

            if (result.Success)
            {
                Report(progress, $"Completed {script.ScriptName}");
                _logger.Information("Completed {Script} in {Elapsed:000} ms", script.ScriptName,
                    stopwatch.ElapsedMilliseconds);
            }
            else
            {
                Report(progress, $"Failed {script.ScriptName} on line {result.LineNumber}: {result.Message}");
                _logger.Error("Failed {Script} in {Elapsed:000} ms", script.ScriptName,
                    stopwatch.ElapsedMilliseconds);

                if (_options.BreakOnError)
                    break;
            }
        }

        Completed(this, new RunWorkerCompletedEventArgs(e.Result, null, e.Cancel));
    }

    private IDatabase GetIDatabaseToUse(IDatabase optionsDatabase, Script script)
    {
        if (!script.SpecifiesDatabase) return optionsDatabase;

        var dbName = script.GetDatabaseToUse(_variables)!;
        return optionsDatabase.GetObjectForDatabaseNamed(dbName);
    }
}
