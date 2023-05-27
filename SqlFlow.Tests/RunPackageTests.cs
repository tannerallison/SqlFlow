using FluentAssertions;
using Moq;

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
            .Returns(new RunResult(true, "Ran successfully"));

        var variables = new List<Variable>();
        var options = new RunOptions { Database = mockDatabase.Object };
        var runPackage = new RunPackage(scripts, variables, options);
        var messages = new List<string>();
        var progress = new List<int>();
        runPackage.ProgressChanged += (sender, args) =>
        {
            progress.Add(args.ProgressPercentage);
            messages.Add(args.UserState?.ToString());
        };

        var worker = runPackage.Run();
        while (worker.IsBusy)
        {
            Thread.Sleep(100);
        }

        progress.Should().Equal(new List<int>{50,50,100,100});

        messages.Should().Contain("Beginning Test1...")
            .And.Contain("Completed Test1")
            .And.Contain("Beginning Test2...")
            .And.Contain("Completed Test2");


    }
}
