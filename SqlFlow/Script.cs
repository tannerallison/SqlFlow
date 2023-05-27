using System.Text.RegularExpressions;

namespace SqlFlow;

public class Script
{
    public static readonly Regex ScriptRegex = new("^_(\\d+)_(.+).sql",
        RegexOptions.IgnoreCase | RegexOptions.Singleline);

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

    public static Script FromString(string scriptText, string path)
    {
        return new Script(scriptText, path);
    }

    public Script(string path) : this(File.ReadAllText(path), path)
    {
    }

    private Script(string scriptText, string path)
    {
        ScriptText = scriptText;
        ScriptPath = path;
        ScriptName = ScriptRegex.Match(path).Groups[2].Value;
        if (!long.TryParse(ScriptRegex.Match(path).Groups[1].Value, out long orderVal))
        {
            throw new Exception("Invalid script name.  Must be in the format _<order>_<name>.sql");
        }

        OrderNumber = orderVal;
        DatabaseToUse = LoadTag(@"\{\{AbstractDatabase=([<>a-z0-9_]+?)\}\}");

        string? trans = LoadTag(@"\{\{Transactional=([a-z0-9_]+?)}}");
        IsTransactional = trans == null || trans.ToLower() == "true";

        LoadMultiTag(@"\{\{subset=([a-z0-9_]+?)}}").ForEach(x => ScriptSets.Add(x));

        LoadMultiTag("<<([a-z0-9_]+?)>>").ForEach(x => ScriptVariables.Add(x));

        if (int.TryParse(LoadTag(@"\{\{Timeout=([0-9]+?)}}"), out int timeoutValue))
            Timeout = timeoutValue;

        Warning = LoadTag(@"\{\{Warn=(.+?)}}");
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
