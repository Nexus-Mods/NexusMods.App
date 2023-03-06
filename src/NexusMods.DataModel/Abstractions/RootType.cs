using NexusMods.DataModel.Loadouts;

namespace NexusMods.DataModel.Abstractions;

/// <summary>
/// Defines the type of <see cref="Root{TRoot}"/> stored within the database.
/// </summary>
/// <remarks>
///    This type is constrained to a byte because the IDs storing
///    these roots are constrained to a byte.
/// </remarks>
public enum RootType : byte
{
    /// <summary>
    /// Root into a loadout, i.e. a Collection of Mods.
    /// Currently represented by <see cref="LoadoutRegistry"/>.
    /// </summary>
    Loadouts = 0,

    /// <summary>
    /// Used in unit tests to validate the functionality of the datastore.
    /// </summary>
    Tests
}
