namespace NexusMods.DataModel.Loadouts.Cursors;

/// <summary>
/// Groups a <see cref="LoadoutId"/> and a <see cref="ModId"/> together, for
/// easy passing around as a single object.
/// </summary>
/// <param name="LoadoutId"></param>
/// <param name="ModId"></param>
public readonly record struct ModCursor(LoadoutId LoadoutId, ModId ModId);
