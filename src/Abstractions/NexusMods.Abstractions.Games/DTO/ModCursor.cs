using NexusMods.Abstractions.DataModel.Entities.Mods;
using NexusMods.Abstractions.Games.Loadouts;

namespace NexusMods.Abstractions.Games.DTO;

/// <summary>
/// Groups a <see cref="LoadoutId"/> and a <see cref="ModId"/> together, for
/// easy passing around as a single object.
/// </summary>
/// <param name="LoadoutId"></param>
/// <param name="ModId"></param>
public readonly record struct ModCursor(LoadoutId LoadoutId, ModId ModId);
