namespace SqlFlow.Manager;

public class ScriptFolder
{
    public ScriptFolder(string path, SearchOption searchOption, string color)
    {
        Path = path;
        SearchOption = searchOption;
        Color = color;
    }

    public string Path { get; set; }
    public SearchOption SearchOption { get; set; }
    public string Color { get; set; }

    public ICollection<Script> GetScriptsFromFolder()
    {
        return Directory.GetFiles(Path, "*.sql", SearchOption)
            .Where(f => Script.ScriptRegex.IsMatch(f))
            .Select(f => new Script(f) { ScriptFolder = this })
            .ToList();
    }
}
