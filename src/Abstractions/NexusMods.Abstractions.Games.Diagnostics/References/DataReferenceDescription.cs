using JetBrains.Annotations;
using TransparentValueObjects;

namespace NexusMods.Abstractions.Diagnostics.References;

/// <summary>
/// Represents the description of a <see cref="IDataReference"/> instance.
/// </summary>
[ValueObject<string>]
[PublicAPI]
public readonly partial struct DataReferenceDescription
{
    /// <summary>
    /// Loadout.
    /// </summary>
    public static readonly DataReferenceDescription Loadout = From("Loadout");

    /// <summary>
    /// Mod.
    /// </summary>
    public static readonly DataReferenceDescription Mod = From("Mod");
}
