using System.Text.Json.Serialization;
using NexusMods.DataModel.JsonConverters;
using Vogen;

namespace NexusMods.DataModel.Loadouts;

/// <summary>
/// A unique identifier for a file which belongs to a mod in the data store/database.
/// These IDs are assigned to files as they are found, i.e. during archive scan/file
/// discovery step.
/// </summary>
[ValueObject<Guid>(conversions: Conversions.None)]
[JsonConverter(typeof(ModFileIdConverter))]
public partial struct ModFileId
{
    /// <summary>
    /// Creates a new <see cref="ModFileId"/> with a unique GUID.
    /// </summary>
    public static ModFileId New()
    {
        return From(Guid.NewGuid());
    }
}
