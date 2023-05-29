namespace SqlFlow.Manager;

public class Subset
{
    public string Name { get; set; }
    public ICollection<Script> Scripts { get; set; } = new List<Script>();
}
