namespace NexusMods.Abstractions.Serialization.DataModel.Ids;

/// <summary>
/// The id of a file analyzer id, which is a combination of a guid and a revision number.
/// </summary>
/// <param name="Analyzer"></param>
/// <param name="Revision"></param>
public record FileAnalyzerId(Guid Analyzer, uint Revision)
{
    /// <summary>
    /// Creates a new file analyzer id.
    /// </summary>
    /// <param name="guid"></param>
    /// <param name="revision"></param>
    /// <returns></returns>
    public static FileAnalyzerId New(string guid, uint revision) => new(Guid.Parse(guid), revision);
}
