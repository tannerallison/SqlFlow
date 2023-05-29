using FluentAssertions;

namespace SqlFlow.Tests;

public class ScriptTests
{
    [Fact]
    public void SubsetsAreParsedWhenPopulated()
    {
        var text = """
        -- {{subset=Test}}
        -- {{subset=Test2}}
        SELECT * FROM TABLE1
        """;

        var script = new Script(@"_1000_Test1.sql", text);

        script.ScriptSets.Should().Contain("Test");
        script.ScriptSets.Should().Contain("Test2");
    }

    [Fact]
    public void VariablesArePopulatedWhenInstantiated()
    {
        var text = """
        SELECT * FROM <<Test_Table>>
        GO

        SELECT '<<AnotherOne>>' FROM TABLE_B
        GO
        """;

        var script = new Script(@"_1000_Test1.sql", text);

        script.ScriptVariables.Should().Contain("Test_Table");
        script.ScriptVariables.Should().Contain("AnotherOne");
    }

    [Fact]
    public void TimeoutIsPopulatedWhenInstantiated()
    {
        var text = """
        -- {{Timeout=100}}
        SELECT * FROM <<Test_Table>>
        GO
        """;

        var script = new Script(@"_1000_Test1.sql", text);

        script.Timeout.Should().Be(100);
    }

    [Fact]
    public void TransactionIsPopulatedWhenInstantiated()
    {
        var text = """
        -- {{Transactional=false}}
        SELECT * FROM <<Test_Table>>
        GO

        SELECT '<<AnotherOne>>' FROM TABLE_B
        GO
        """;

        var script = new Script(@"_1000_Test1.sql", text);

        script.IsTransactional.Should().Be(false);
    }

    [Fact]
    public void TransactionIsFalseForAnyValueExceptTrue()
    {
        var text = """
        -- {{Transactional=avsvsdv}}
        SELECT * FROM <<Test_Table>>
        GO

        SELECT '<<AnotherOne>>' FROM TABLE_B
        GO
        """;

        var script = new Script(@"_1000_Test1.sql", text);

        script.IsTransactional.Should().Be(false);
    }

    [Fact]
    public void TransactionDefaultsToTrueIfNotSet()
    {
        var text = """
        SELECT * FROM <<Test_Table>>
        GO

        SELECT '<<AnotherOne>>' FROM TABLE_B
        GO
        """;

        var script = new Script(@"_1000_Test1.sql", text);

        script.IsTransactional.Should().Be(true);
    }

    [Fact]
    public void SpecifiesDatabaseDefaultsToFalseIfNotSet()
    {
        var text = """
        SELECT * FROM <<Test_Table>>
        GO
        """;

        var script = new Script(@"_1000_Test1.sql", text);

        script.SpecifiesDatabase.Should().Be(false);
    }

    [Fact]
    public void SpecifiesDatabaseIsTrueWhenAbstractDatabaseIsSet()
    {
        var text = """
        -- {{AbstractDatabase=TestDB}}
        SELECT * FROM <<Test_Table>>
        GO

        SELECT '<<AnotherOne>>' FROM TABLE_B
        GO
        """;

        var script = new Script(@"_1000_Test1.sql", text);

        script.SpecifiesDatabase.Should().Be(true);
    }

    [Fact]
    public void DatabaseAllowsForVariableReplacement()
    {
        var text = """
        -- {{AbstractDatabase=<<Variable_Db>>}}
        SELECT * FROM <<Test_Table>>
        GO

        SELECT '<<AnotherOne>>' FROM TABLE_B
        GO
        """;

        var script = new Script(@"_1000_Test1.sql", text);
        var variables = new List<Variable> { new("Variable_Db") { Value = "TestDB" } };

        script.GetDatabaseToUse(variables).Should().Be("TestDB");
    }

    [Fact]
    public void DatabaseIsPopulatedWhenInstantiated()
    {
        var text = """
        -- {{AbstractDatabase=TestDB}}
        SELECT * FROM <<Test_Table>>
        GO

        SELECT '<<AnotherOne>>' FROM TABLE_B
        GO
        """;

        var script = new Script(@"_1000_Test1.sql", text);

        script.GetDatabaseToUse(new List<Variable>()).Should().Be("TestDB");
    }

    [Fact]
    public void WarnIsPopulatedWhenInstantiated()
    {
        var text = """
        -- {{Warn=This is a warning}}
        SELECT * FROM <<Test_Table>>
        GO
        """;

        var script = new Script(@"_1000_Test1.sql", text);

        script.Warning.Should().Be("This is a warning");
    }
}
