namespace SqlFlow;

public class ParsedSubQuery
{
    public string Query { get; set; }

    /// <summary>
    /// The starting line number of the query within the larger script
    /// </summary>
    public int LineNumber { get; set; }
}