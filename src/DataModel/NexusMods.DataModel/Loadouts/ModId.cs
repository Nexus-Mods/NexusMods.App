using System.Text.Json.Serialization;
using NexusMods.DataModel.JsonConverters;
using TransparentValueObjects;

namespace NexusMods.DataModel.Loadouts;

/// <summary>
/// A unique identifier for the mod for use within the data store/database.
/// These IDs are assigned to mods upon installation (i.e. when a mod is
/// added to a loadout), or when a tool generates some files after running.
/// </summary>
[ValueObject<Guid>]
[JsonConverter(typeof(ModIdConverter))]
public readonly partial struct ModId { }
