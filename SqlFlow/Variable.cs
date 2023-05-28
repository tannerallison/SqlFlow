namespace SqlFlow;

public class Variable
{
    public Variable(string key)
    {
        Key = key;
        Scripts = new HashSet<Script>();
    }

    public string Key { get; set; }
    public string Value { get; set; }
    public bool Editable { get; set; }
    public ISet<Script> Scripts { get; set; }
}

public class Subset
{
    public string Name { get; set; }
    public ICollection<Script> Scripts { get; set; }
}
