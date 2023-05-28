using FluentAssertions;
using Moq;
using SqlFlow.Database;

namespace SqlFlow.Tests;

public class RunPackageTests
{
    [Fact]
    public void RunPackageShouldRunAllScripts()
    {
        var scripts = new List<Script>
        {
            Script.FromString("SELECT * FROM TABLE1", @"_1000_Test1.sql"),
            Script.FromString("SELECT * FROM TABLE2", @"_1010_Test2.sql"),
        };
        Mock<IDatabase> mockDatabase = new();
        mockDatabase
            .Setup(x => x.ExecuteCommand(It.IsAny<string>(), It.IsAny<QueryOptions>()))
            .Returns(new DbExecutionResult(true, "Ran successfully"));

        var variables = new List<Variable>();
        var progress1 = new Progress<RunProgress>();
        var options = new RunOptions
        {
            Database = mockDatabase.Object,
            Progress = progress1
        };
        var runPackage = new RunPackage(scripts, variables, options);
        var messages = new List<string>();
        var progress = new List<int>();
        progress1.ProgressChanged += (sender, runProgress) =>
        {
            progress.Add(runProgress.Percent);
            messages.Add(runProgress.Message);
        };

        var task = runPackage.Run();

        progress.Should().Equal(new List<int> { 50, 50, 100, 100 });

        messages.Should().Contain("Beginning Test1...")
            .And.Contain("Completed Test1")
            .And.Contain("Beginning Test2...")
            .And.Contain("Completed Test2");
    }
}
