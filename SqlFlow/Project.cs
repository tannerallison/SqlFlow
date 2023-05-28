namespace SqlFlow;

public class Project
{
    public string Name { get; set; } = "";

    /// <summary>
    /// Folders where scripts are located. These are set up as cascading folders, so the scripts of the same name
    /// in the later folders will override the scripts in the earlier folders.
    /// </summary>
    public List<ScriptFolder> ScriptFolders { get; set; } = new();

    public HashSet<Variable> Variables { get; set; } = new();

    public HashSet<string> Subsets { get; set; } = new();

    public IDatabase Database { get; set; }
}
