namespace SqlFlow;

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
}
