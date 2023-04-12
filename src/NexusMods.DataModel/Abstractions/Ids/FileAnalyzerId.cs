namespace NexusMods.DataModel.Abstractions.Ids;

public record FileAnalyzerId(Guid Analyzer, uint Revision)
{
    public static FileAnalyzerId New(string guid, uint revision) => new(Guid.Parse(guid), revision);
}
