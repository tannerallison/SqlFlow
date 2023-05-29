using System.Text.RegularExpressions;

namespace SqlFlow.Manager;

public class Script
{
    public static readonly Regex ScriptRegex = new("^_(\\d+)_(.+).sql",
        RegexOptions.IgnoreCase | RegexOptions.Singleline);

    private const string TagDatabase = @"\{\{AbstractDatabase=([<>a-z0-9_]+?)\}\}";
    private const string TagTransaction = @"\{\{Transactional=([a-z0-9_]+?)}}";
    private const string TagSubset = @"\{\{subset=([a-z0-9_]+?)}}";
    private const string TagVariable = @"<<([a-z0-9_]+?)>>";
    private const string TagTimeout = @"\{\{Timeout=([0-9]+?)}}";
    private const string TagWarning = @"\{\{Warn=(.+?)}}";

    public string ScriptPath { get; }
    public long OrderNumber { get; }
    public string ScriptName { get; }
    public string ScriptText { get; }

    public ScriptFolder? ScriptFolder { get; init; }

    public HashSet<string> ScriptSets { get; } = new();
    public HashSet<string> ScriptVariables { get; } = new();

    public int? Timeout { get; }
    public string? Warning { get; }
    public bool IsTransactional { get; }
    private string? DatabaseToUse { get; }

    public Script(string scriptPath) : this(scriptPath, ParseOrderNumberFromPath(scriptPath),
        ParseNameFromPath(scriptPath), ReadScriptText(scriptPath))
    {
    }

    public Script(string scriptPath, string scriptText) : this(scriptPath, ParseOrderNumberFromPath(scriptPath),
        ParseNameFromPath(scriptPath), scriptText)
    {
    }

    public Script(string scriptPath, long orderNumber, string scriptName, string scriptText, ScriptFolder? scriptFolder = null)
    {
        ScriptName = scriptName;
        ScriptPath = scriptPath;
        ScriptText = scriptText;
        OrderNumber = orderNumber;
        ScriptFolder = scriptFolder;

        DatabaseToUse = LoadTag(TagDatabase);

        string? trans = LoadTag(TagTransaction);
        IsTransactional = trans == null || trans.ToLower() == "true";

        LoadMultiTag(TagSubset).ForEach(x => ScriptSets.Add(x));

        LoadMultiTag(TagVariable).ForEach(x => ScriptVariables.Add(x));

        if (int.TryParse(LoadTag(TagTimeout), out int timeoutValue))
            Timeout = timeoutValue;

        Warning = LoadTag(TagWarning);
    }

    private static string ReadScriptText(string path) => File.ReadAllText(path);

    private static string ParseNameFromPath(string path) => ScriptRegex.Match(path).Groups[2].Value;

    private static long ParseOrderNumberFromPath(string path)
    {
        if (!long.TryParse(ScriptRegex.Match(path).Groups[1].Value, out long orderVal))
        {
            throw new Exception("Invalid script name.  Must be in the format _<order>_<name>.sql");
        }

        return orderVal;
    }

    public string GetReplacedText(ICollection<Variable> variables) => ReplaceVariables(ScriptText, variables);

    private string ReplaceVariables(string value, ICollection<Variable> variables)
    {
        string ReplaceVariable(string current, Variable v) => current.Replace($"<<{v.Key}>>", v.Value);

        return variables.Aggregate(value, ReplaceVariable);
    }

    public bool SpecifiesDatabase => DatabaseToUse != null;

    /// <summary>
    /// Pass in a list of populated variables in order to allow databases defined as a variable to be
    /// populated from the value of the variable.
    /// </summary>
    /// <param name="variables"></param>
    /// <returns></returns>
    public string? GetDatabaseToUse(ICollection<Variable> variables)
    {
        return DatabaseToUse == null
            ? null
            : ReplaceVariables(DatabaseToUse, variables);
    }

    private List<string> LoadMultiTag(string regex, int grouping = 1)
    {
        var re = new Regex(regex, RegexOptions.IgnoreCase | RegexOptions.Singleline);

        MatchCollection matchCollection = re.Matches(ScriptText);

        return (from Match match in matchCollection select match.Groups[grouping].Value).Distinct().ToList();
    }

    private string? LoadTag(string regex)
    {
        return LoadMultiTag(regex, 1).FirstOrDefault()?.Replace("--", "");
    }
}
