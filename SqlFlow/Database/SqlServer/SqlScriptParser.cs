using System.Text;
using System.Text.RegularExpressions;

namespace SqlFlow.Database.SqlServer;

public static class SqlScriptParser
{
    public static IList<ParsedSubQuery> ParseScript(string script)
    {
        string commandSql = script.ReplaceLineEndings().TrimEnd();

        if (commandSql.EndsWith(Environment.NewLine + "GO"))
        {
            commandSql = commandSql.Substring(0, commandSql.Length - 2);
        }

        // Split the query by the GO text
        var queries = new List<ParsedSubQuery>();
        string[] lines = commandSql.Split(Environment.NewLine);

        var query = new StringBuilder();
        bool multComment = false;
        bool debugSection = false;

        int lineNum = 0;
        int queryLine = 1;
        foreach (string lineItem in lines)
        {
            lineNum++;
            string line = lineItem.Trim();

            // Comment out all code in debug sections
            if (debugSection)
                line = "-- " + line;
            if (line.ToLower().StartsWith(@"--#debug"))
                // If it starts with "--#debug" and ends with "#enddebug" on one line, it will be commented out anyway
                debugSection = true;
            if (line.ToLower().EndsWith(@"#enddebug"))
                debugSection = false;

            // If there is a multi-line comment within a single line, just strip it out.
            line = Regex.Replace(line, @"/\*.*?\*/", "");

            // We don't want to match a "GO" if it's within a multi-line comment
            if (line.Contains("/*"))
                multComment = true;
            if (line.Contains("*/"))
                multComment = false;

            query.AppendLine(line);

            if (debugSection || multComment || line.StartsWith("--") || string.IsNullOrEmpty(line) ||
                !line.ToUpper().Equals("GO"))
                continue;

            var queryText = TrimQuery(query);

            if (!string.IsNullOrEmpty(queryText)) // Don't include empty queries
                queries.Add(new ParsedSubQuery(queryText, queryLine));

            queryLine = lineNum + 1;
            query.Clear();
        }

        string lastQuery = TrimQuery(query);
        if (!string.IsNullOrEmpty(lastQuery)) // Don't include empty queries
            queries.Add(new ParsedSubQuery(lastQuery, queryLine));

        return queries.AsReadOnly();
    }

    private static string TrimQuery(StringBuilder query) =>
        Regex.Replace(query.ToString().Trim(), @"^GO\b", "",
            RegexOptions.Multiline | RegexOptions.IgnoreCase).Trim();
}
