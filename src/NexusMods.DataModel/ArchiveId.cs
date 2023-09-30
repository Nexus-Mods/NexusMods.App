using Vogen;

namespace NexusMods.DataModel;

/// <summary>
/// A unique identifier for an archive in a ArchiveManager
/// </summary>
[ValueObject<Guid>]
public partial struct ArchiveId
{

    /// <summary>
    /// Create a new archive id, randomly generated
    /// </summary>
    /// <returns></returns>
    public static ArchiveId New() => From(Guid.NewGuid());

}
