using FluentAssertions;

namespace SqlFlow.Tests;

public class SqlScriptParserTests
{
    [Fact]
    public void SqlBoundedByDebugSectionsIsIgnored()
    {
        var query = """
            SELECT * FROM TABLE1
            GO
            --#debug
            SELECT * FROM BADTABLE
            GO
            --#enddebug
            SELECT * FROM TABLE2
            GO
            """;
        var parsedQuery = SqlScriptParser.ParseScript(query);
        parsedQuery.Count.Should().Be(2);

    }

    [Fact]
    public void ScriptWithGoLineSplitsIntoMultipleQueries()
    {
        var query = """
            SELECT * FROM TABLE1
            GO

            SELECT * FROM TABLE2
            GO
            """;
        var parsedQuery = SqlScriptParser.ParseScript(query);
        parsedQuery.Count.Should().Be(2);
    }

    [Fact]
    public void ScriptWithGoLineSplitsIntoMultipleQueriesWithLineNumbers()
    {
        var query = """
            SELECT * FROM TABLE1
            GO

            SELECT * FROM TABLE2
            GO
            """;
        var parsedQuery = SqlScriptParser.ParseScript(query);
        parsedQuery.First().LineNumber.Should().Be(1);
        parsedQuery.Last().LineNumber.Should().Be(3);
    }

    [Fact]
    public void GoShouldBeStrippedFromQuery()
    {
        var query = """
            SELECT * FROM TABLE1
            GO

            SELECT * FROM TABLE2
            GO
            """;
        var parsedQuery = SqlScriptParser.ParseScript(query);
        parsedQuery.First().Query.Should().Be("SELECT * FROM TABLE1");
        parsedQuery.Last().Query.Should().Be("SELECT * FROM TABLE2");
    }

    [Fact]
    public void MultilineCommentsDoNotResultInQueries()
    {
        var query = """
            SELECT * FROM TABLE1
            GO
            /* This is a
               multiline comment
               SELECT * FROM BADTABLE
               GO
               */

            SELECT * FROM TABLE2
            GO
            """;
        var parsedQuery = SqlScriptParser.ParseScript(query);
        parsedQuery.Last().Query.Should().Be(@"/* This is a
multiline comment
SELECT * FROM BADTABLE

*/

SELECT * FROM TABLE2");
    }
}
