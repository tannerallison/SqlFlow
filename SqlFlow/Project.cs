using System.Collections.Concurrent;
using SqlFlow.Database;

namespace SqlFlow;

public class Project
{
    public string Name { get; set; } = "";

    /// <summary>
    /// Folders where scripts are located. These are set up as cascading folders, so the scripts of the same name
    /// in the later folders will override the scripts in the earlier folders.
    /// </summary>
    public List<ScriptFolder> ScriptFolders { get; set; } = new();

    public void AddScriptFolder(string path, SearchOption searchOption, string color)
    {
        ScriptFolders.Add(new ScriptFolder(path, searchOption, color));
    }

    private void ScanFolderAndAddScripts(string directory, SearchOption searchOption, string color)
    {
        var files = Directory.GetFiles(directory, "*.sql", searchOption);
        foreach (var path in files)
        {
            if (Script.ScriptRegex.IsMatch(path))
            {
                var script = new Script(path);
                foreach (var variable in script.ScriptVariables)
                {
                    Variables.AddOrUpdate(variable, v =>
                    {
                        v.Scripts.Add(script);
                        return v;
                    }, () => new Variable(variable));
                }
            }
        }
    }

    public Dictionary<string, Variable> Variables { get; set; } = new();

    public Dictionary<string, Subset> Subsets { get; set; } = new();

    public IDatabase Database { get; set; }
}
