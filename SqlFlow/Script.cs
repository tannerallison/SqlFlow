using System.Text.RegularExpressions;

namespace SqlFlow;

public class Script
{
    public override bool Equals(object obj)
    {
        return obj.GetType() == typeof(Script) && Equals((Script)obj);
    }

    public bool Equals(Script other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Equals(other.ScriptPath, ScriptPath);
    }

    public override int GetHashCode()
    {
        // ReSharper disable NonReadonlyFieldInGetHashCode
        // We want this to be alterable so two Script objects with the same _scriptText will show as the same.
        return ScriptPath.GetHashCode();
        // ReSharper restore NonReadonlyFieldInGetHashCode
    }

    public static readonly Regex ScriptRegex = new("^_(\\d+)_(.+).sql",
        RegexOptions.IgnoreCase | RegexOptions.Singleline);

    private const string TagDatabase = @"\{\{AbstractDatabase=([<>a-z0-9_]+?)\}\}";
    private const string TagTransaction = @"\{\{Transactional=([a-z0-9_]+?)}}";
    private const string TagSubset = @"\{\{subset=([a-z0-9_]+?)}}";
    private const string TagVariable = @"<<([a-z0-9_]+?)>>";
    private const string TagTimeout = @"\{\{Timeout=([0-9]+?)}}";
    private const string TagWarning = @"\{\{Warn=(.+?)}}";

    public string ScriptText { get; }

    public HashSet<string> ScriptSets { get; } = new();
    public HashSet<string> ScriptVariables { get; } = new();

    public int? Timeout { get; }
    public string? Warning { get; }
    public bool IsTransactional { get; }
    public string ScriptName { get; }
    public string ScriptPath { get; }
    public long OrderNumber { get; }
    private string? DatabaseToUse { get; }

    public Script(string scriptPath) : this(scriptPath, ParseOrderNumberFromPath(scriptPath),
        ParseNameFromPath(scriptPath), ReadScriptText(scriptPath))
    {
    }

    public Script(string scriptPath, string scriptText) : this(scriptPath, ParseOrderNumberFromPath(scriptPath),
        ParseNameFromPath(scriptPath), scriptText)
    {
    }

    public Script(string scriptPath, long orderNumber, string scriptName, string scriptText)
    {
        ScriptName = scriptName;
        ScriptPath = scriptPath;
        ScriptText = scriptText;
        OrderNumber = orderNumber;

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
