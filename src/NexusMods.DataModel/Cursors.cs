using NexusMods.DataModel.Loadouts;

namespace NexusMods.DataModel;

/// <summary>
/// A logical
/// </summary>
/// <param name="LoadoutId"></param>
public record struct LoadoutCursor(LoadoutId LoadoutId);

public record struct ModCursor(LoadoutId LoadoutId, ModId ModId);
