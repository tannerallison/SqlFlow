using System.Text.RegularExpressions;

namespace SqlFlow;

public class Script
{
    public static readonly Regex ScriptRegex = new("^(_\\d+_)(.+).sql",
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
    public string? DatabaseToUse { get; }

    public Script(string path, string name)
    {
        ScriptPath = path;
        ScriptName = ScriptRegex.Match(name).Groups[2].Value;
        if (!long.TryParse(ScriptRegex.Match(path).Groups[1].Value, out long orderVal))
        {
            throw new Exception("Invalid script name.  Must be in the format _<order>_<name>.sql");
        }

        OrderNumber = orderVal;
        ScriptText = File.ReadAllText(path);
        DatabaseToUse = LoadTag(@"\{\{AbstractDatabase=([<>a-z0-9_]+?)\}\}");

        string? trans = LoadTag(@"\{\{Transactional=([a-z0-9_]+?)}}");
        IsTransactional = trans == null || trans.ToLower() == "true";

        LoadMultiTag(@"\{\{subset=([a-z0-9_]+?)}}").ForEach(x => ScriptSets.Add(x));

        LoadMultiTag("<<([a-z0-9_]+?)>>").ForEach(x => ScriptVariables.Add(x));

        if (int.TryParse(LoadTag(@"\{\{Timeout=([0-9]+?)}}"), out int timeoutValue))
            Timeout = timeoutValue;

        Warning = LoadTag(@"\{\{Warn=(.+?)}}");
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
