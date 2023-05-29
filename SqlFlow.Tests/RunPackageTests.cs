using FluentAssertions;
using Moq;
using Serilog;
using SqlFlow.Database;

namespace SqlFlow.Tests;

public class RunPackageTests
{
    [Fact]
    public void RunPackageShouldRunAllScripts()
    {
        var scripts = new List<Script>
        {
            new(@"_1000_Test1.sql", @"SELECT * FROM TABLE1"),
            new(@"_1010_Test2.sql", @"SELECT * FROM TABLE2"),
        };
        Mock<IDatabase> mockDatabase = new();
        mockDatabase
            .Setup(x => x.ExecuteCommand(It.IsAny<string>(), It.IsAny<QueryOptions>()))
            .Returns(new DbExecutionResult(true, "Ran successfully"));

        var variables = new List<Variable>();
        var progress1 = new Progress<RunProgress>();

        var options = new RunOptions
        {
            Progress = progress1,
            Logger = new LoggerConfiguration().WriteTo.Console().CreateLogger()
        };
        var runPackage = new RunPackage(scripts, variables, mockDatabase.Object, options);
        var messages = new List<string>();
        var progress = new List<int>();
        progress1.ProgressChanged += (sender, runProgress) =>
        {
            progress.Add(runProgress.Percent);
            messages.Add(runProgress.Message);
        };

        var task = runPackage.Run();

        progress.Should().Equal(new List<int> { 50, 50, 100, 100 });

        messages.Should().Contain("Executing Test1...")
            .And.Contain("Completed Test1")
            .And.Contain("Executing Test2...")
            .And.Contain("Completed Test2");
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
