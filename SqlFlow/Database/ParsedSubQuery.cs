namespace SqlFlow.Database;

public class ParsedSubQuery
{
    public ParsedSubQuery(string query, int lineNumber)
    {
        Query = query;
        LineNumber = lineNumber;
    }

    public string Query { get; }

    /// <summary>
    /// The starting line number of the query within the larger script
    /// </summary>
    public int LineNumber { get; }
}
