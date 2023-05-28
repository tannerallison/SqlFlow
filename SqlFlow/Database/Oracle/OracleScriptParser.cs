using System.Text;

namespace SqlFlow.Database.Oracle;

public static class OracleScriptParser
{
    public static IList<ParsedSubQuery> ParseScript(string script)
    {
        // Split the query by the COMMIT text
        var queries = new List<ParsedSubQuery>();

//          Regex reg = new Regex(@"(?s)/\*(.*?)\*/", RegexOptions.Multiline); // This is an option, but doing it messes with the line numbers
//		    script = reg.Replace(script, " ");

        string[] lines = script.Split(new[] { "\r\n" }, StringSplitOptions.None);
        var query = new StringBuilder();
        bool dec = false;
        bool multilineComment = false;
        int beg = 0;
        int lineNum = 0;
        int queryLine = 1;
        foreach (string lineItem in lines)
        {
            lineNum++;

            string line = lineItem;

            // This will handle multiline comments unless somebody puts multiple multi-line comments on the same line.
            // If someone /* does something */ like this, /* my parser won't */ be able to handle it.
            if (multilineComment) // If we're in the midst of a multi-line comment, ignore the current line
            {
                line = "";
            }
            else if (lineItem.Contains("/*")) // If we're in the middle of a multi-line comment
            {
                multilineComment = true;
                line = lineItem.Substring(0, lineItem.IndexOf("/*", StringComparison.Ordinal));
            }

            if (lineItem.Contains("*/"))
            {
                multilineComment = false;
                line += lineItem.Substring(lineItem.LastIndexOf("*/", System.StringComparison.Ordinal) + 2);
            }

            if (line.Trim().StartsWith("--")
                || string.IsNullOrWhiteSpace(line))
                continue;


            query.AppendLine(line.Trim());
            if (line.Trim().EndsWith(";")
                && dec == false
                && beg == 0)
            {
                queries.Add(new ParsedSubQuery
                    { LineNumber = queryLine, Query = query.Replace(";", "\r\n").ToString().Trim() });
                queryLine = lineNum + 1;
                query.Clear();
            }
            else if (line.ToUpper().StartsWith("DECLARE"))
            {
                dec = true;
            }
            else if (line.ToUpper().StartsWith("/") && dec)
            {
                dec = false;
                queries.Add(new ParsedSubQuery
                    { LineNumber = queryLine, Query = query.ToString().Trim().TrimEnd('/', ';') });
                queryLine = lineNum + 1;
                query.Clear();
            }
            else if (line.ToUpper().Trim().TrimEnd('/', ';').Equals("BEGIN")
                     && dec == false)
            {
                beg++;
            }
            else if (line.ToUpper().Trim().TrimEnd('/', ';', ' ').Equals("END")
                     && dec == false)
            {
                beg--;
                // if the number of BEGIN statements equals zero, then we've reached the last END, parse out the script.
                if (beg == 0)
                {
                    queries.Add(new ParsedSubQuery
                        { LineNumber = queryLine, Query = query.ToString().Trim().TrimEnd('/') });
                    queryLine = lineNum + 1;
                    query.Clear();
                }
            }
        }

        if (beg != 0)
            throw new Exception("The script did not parse correctly. A BEGIN statement is missing an END;");

        if (dec)
            throw new Exception("The script did not parse correctly. A DECLARE did not have a '/'");

        return queries;
    }
}
