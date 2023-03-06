namespace NexusMods.Benchmarks.Interfaces;

public class BenchmarkInfoAttribute : Attribute
{
    public string? Name { get; set; }
    public string? Description { get; set; }

    public BenchmarkInfoAttribute(string? name, string? description)
    {
        Name = name;
        Description = description;
    }
}
