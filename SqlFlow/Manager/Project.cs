using Newtonsoft.Json;
using Serilog;
using SqlFlow.Database;

namespace SqlFlow.Manager;



public class Project
{
    public static string Serialize(Project project)
    {
        return JsonConvert.SerializeObject(project, Formatting.Indented,
            new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto });
    }

    public static Project Deserialize(string serializedProject)
    {
        var project = JsonConvert.DeserializeObject<Project>(serializedProject);
        if (project == null)
            throw new Exception("Failed to load project");

        project.PopulateFromScriptFolders();

        return project;
    }

    public Project()
    {
    }

    public string Name { get; set; } = "";

    public ILogger? Logger { get; set; }

    private string? _directoryPath;

    public string DirectoryPath
    {
        get => _directoryPath ?? AppDomain.CurrentDomain.BaseDirectory;
        set => _directoryPath = value;
    }

    /// <summary>
    /// Folders where scripts are located. These are set up as cascading folders, so the scripts of the same name
    /// in the later folders will override the scripts in the earlier folders.
    /// </summary>
    public readonly List<ScriptFolder> ScriptFolders = new();

    public void AddScriptFolder(string path, SearchOption searchOption, string color)
    {
        ScriptFolders.Add(new ScriptFolder(path, searchOption, color));
        PopulateFromScriptFolders();
    }

    public void PopulateFromScriptFolders()
    {
        Scripts.Clear();
        Subsets.Clear();
        Variables.ForEach(v => v.Value.Scripts.Clear());
        var scriptFolders = ScriptFolders.ToList();
        scriptFolders.Reverse();
        foreach (var scriptFolder in scriptFolders)
        {
            var scriptsFromFolder = scriptFolder.GetScriptsFromFolder();
            foreach (var script in scriptsFromFolder)
            {
                if (Scripts.Any(s => s.ScriptName == script.ScriptName && s.OrderNumber == script.OrderNumber))
                {
                    Logger?.Verbose(
                        "Skipping script {OrderNumber} {ScriptName} from folder {ScriptFolder}; overridden in more granular folder",
                        script.OrderNumber, script.ScriptName);
                    continue;
                }

                Logger?.Verbose("Adding script {OrderNumber} {ScriptName} from folder {ScriptFolder}",
                    script.OrderNumber, script.ScriptName, scriptFolder);

                Scripts.Add(script);
                script.ScriptSets.ForEach(s => Subsets.AddOrUpdate(s, subset =>
                {
                    subset.Scripts.Add(script);
                    return subset;
                }, () => new Subset { Name = s, Scripts = new List<Script> { script } }));
                script.ScriptVariables.ForEach(v => Variables.AddOrUpdate(v, variable =>
                {
                    variable.Scripts.Add(script);
                    return variable;
                }, () => new Variable(v) { Scripts = new HashSet<Script> { script } }));
            }
        }

        Variables.RemoveWhere(c => c.Value.Scripts.None());
    }

    [NonSerialized] public readonly List<Script> Scripts = new();

    [NonSerialized] public readonly Dictionary<string, Subset> Subsets = new();

    public readonly Dictionary<string, Variable> Variables = new();

    public IDatabase Database { get; set; }

    public bool IsDirty { get; set; }
}
