using Vogen;

namespace NexusMods.DataModel;

/// <summary>
/// Id for a registered download
/// </summary>
[ValueObject<Guid>]
public partial struct DownloadId
{
    /// <summary>
    /// Create a new download id, randomly generated
    /// </summary>
    /// <returns></returns>
    public static DownloadId New() => From(Guid.NewGuid());

}
