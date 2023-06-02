using FluentAssertions;
using Moq;
using Serilog;
using SqlFlow.Database;
using SqlFlow.Manager;
using SqlFlow.Runner;

namespace SqlFlow.Tests;

public class RunPackageTests
{
    private readonly Mock<IDatabase> _mockDatabase;
    private readonly IEnumerable<Script> _scripts;
    private readonly ICollection<Variable> _variables;
    private readonly Mock<ILogger> _mockLogger;

    public RunPackageTests()
    {
        _mockDatabase = new Mock<IDatabase>();
        _scripts = new List<Script>
        {
            new Script("_0010_Script1.sql", "SELECT * FROM Table1;"),
            new Script("_0020_Script2.sql", "SELECT * FROM Table2;")
        };
        _variables = new List<Variable>();
        _mockLogger = new Mock<ILogger>();
    }

    [Fact]
    public void Run_ShouldExecuteAllScripts()
    {
        // Arrange
        var runPackage = new RunPackage(_scripts, _variables, _mockDatabase.Object, new RunOptions());
        _mockDatabase.Setup(d => d.ExecuteCommand(It.IsAny<string>(), It.IsAny<QueryOptions>()))
            .Returns(new DbExecutionResult(true, "Script Executed successfully"));

        // Act
        var runResult = runPackage.Run();

        // Assert
        Assert.Equal(2, runResult.Results.Count);
        _mockDatabase.Verify(d => d.ExecuteCommand(It.IsAny<string>(), It.IsAny<QueryOptions>()), Times.Exactly(2));
    }

    [Fact]
    public void Run_ShouldCancelExecutionWhenCancellationRequested()
    {
        // Arrange
        var cancellationTokenSource = new CancellationTokenSource();
        var runPackage = new RunPackage(_scripts, _variables, _mockDatabase.Object,
            new RunOptions { CancellationToken = cancellationTokenSource.Token, Logger = _mockLogger.Object });

        // Act
        cancellationTokenSource.Cancel();
        var runResult = runPackage.Run();

        // Assert
        Assert.True(runResult.Cancelled);
        Assert.Empty(runResult.Results);
        _mockLogger.Verify(l => l.Information("Cancelled prior to {Script}", It.IsAny<string>()), Times.Exactly(1));
        _mockDatabase.Verify(d => d.ExecuteCommand(It.IsAny<string>(), It.IsAny<QueryOptions>()), Times.Never);
    }

    [Fact]
    public void Run_ShouldStopExecutionOnErrorIfBreakOnErrorOptionIsSet()
    {
        // Arrange
        _mockDatabase.Setup(d => d.ExecuteCommand("SELECT * FROM Table1;", It.IsAny<QueryOptions>()))
            .Returns(new DbExecutionResult(true, ""));
        _mockDatabase.Setup(d => d.ExecuteCommand("SELECT * FROM Table2;", It.IsAny<QueryOptions>()))
            .Returns(new DbExecutionResult(false, "Invalid object name 'Table2'"));
        var runPackage = new RunPackage(_scripts, _variables, _mockDatabase.Object,
            new RunOptions { BreakOnError = true });

        // Act
        var runResult = runPackage.Run();

        // Assert
        Assert.Equal(2, runResult.Results.Count);
        _mockDatabase.Verify(d => d.ExecuteCommand(It.IsAny<string>(), It.IsAny<QueryOptions>()), Times.Exactly(2));
    }


    [Fact]
    public void Run_ShouldReportProgressViaProgressObject()
    {
        _mockDatabase
            .Setup(x => x.ExecuteCommand(It.IsAny<string>(), It.IsAny<QueryOptions>()))
            .Returns(new DbExecutionResult(true, "Ran successfully"));

        var progress1 = new Progress<RunProgress>();

        var options = new RunOptions
        {
            Progress = progress1,
            Logger = _mockLogger.Object
        };
        var runPackage = new RunPackage(_scripts, _variables, _mockDatabase.Object, options);
        var progress = new List<RunProgress>();
        progress1.ProgressChanged += (sender, runProgress) => { progress.Add(runProgress); };

        runPackage.Run();

        progress.Should().Equal(new List<RunProgress>
        {
            new(50, "Executing Script1..."),
            new(50, "Completed Script1"),
            new(100, "Executing Script2..."),
            new(100, "Completed Script2")
        });
    }


    [Fact]
    public void OtherDatabaseIsUsedWhenScriptSpecifies()
    {
        var text = """
        -- {{AbstractDatabase=<<DB_TO_USE>>}}
        SELECT * FROM TABLE1
        """;
        var scripts = new List<Script> { new(@"_1000_Test1.sql", text) };
        var variables = new List<Variable> { new("DB_TO_USE") { Value = "OtherDatabase" } };

        Mock<IDatabase> secondaryDatabase = new();
        secondaryDatabase
            .Setup(x => x.ExecuteCommand(It.IsAny<string>(), It.IsAny<QueryOptions>()))
            .Returns(new DbExecutionResult(true, "Ran successfully"));

        Mock<IDatabase> mockDatabase = new();
        mockDatabase
            .Setup(x => x.GetObjectForDatabaseNamed("OtherDatabase"))
            .Returns(secondaryDatabase.Object);

        new RunPackage(scripts, variables, mockDatabase.Object).Run();

        mockDatabase.Verify(x => x.GetObjectForDatabaseNamed("OtherDatabase"));

        secondaryDatabase.Verify(x => x.ExecuteCommand(It.IsAny<string>(), It.IsAny<QueryOptions>()), Times.Once);
    }
}
