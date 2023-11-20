using System.Text.Json.Serialization;
using NexusMods.DataModel.JsonConverters;
using TransparentValueObjects;

namespace NexusMods.DataModel.Loadouts;

/// <summary>
/// A unique identifier for a file which belongs to a mod in the data store/database.
/// These IDs are assigned to files as they are found, i.e. during archive scan/file
/// discovery step.
/// </summary>
[ValueObject<Guid>]
[JsonConverter(typeof(ModFileIdConverter))]
public readonly partial struct ModFileId { }
