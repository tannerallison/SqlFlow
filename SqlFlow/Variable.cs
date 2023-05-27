namespace SqlFlow;

public class Variable
{
    public string Key { get; set; }
    public string Value { get; set; }
    public bool Editable { get; set; }
    public string[] Scripts { get; set; }
}
