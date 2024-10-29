using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.ValueSerializers;
using TransparentValueObjects;

namespace NexusMods.Abstractions.GameLocators;

/// <summary>
/// Represents a game store from which the game installation originates.
/// </summary>
[ValueObject<string>]
[PublicAPI]
[SuppressMessage("ReSharper", "InconsistentNaming")]
public readonly partial struct GameStore
{
    /// <summary>
    /// Unknown.
    /// </summary>
    public static readonly GameStore Unknown = From("Unknown");

    /// <summary>
    /// Steam.
    /// </summary>
    public static readonly GameStore Steam = From("Steam");

    /// <summary>
    /// GOG.
    /// </summary>
    public static readonly GameStore GOG = From("GOG");

    /// <summary>
    /// EGS.
    /// </summary>
    public static readonly GameStore EGS = From("Epic Games Store");

    /// <summary>
    /// Origin.
    /// </summary>
    public static readonly GameStore Origin = From("Origin");

    /// <summary>
    /// EA Desktop.
    /// </summary>
    public static readonly GameStore EADesktop = From("EA Desktop");

    /// <summary>
    /// Xbox Game Pass.
    /// </summary>
    public static readonly GameStore XboxGamePass = From("Xbox Game Pass");

    /// <summary>
    /// Manually added.
    /// </summary>
    public static readonly GameStore ManuallyAdded = From("Manually Added");
}

/// <summary>
/// An attribute that contains the name of a game store.
/// </summary>
public class GameStoreAttribute(string ns, string name) : ScalarAttribute<GameStore, string, AsciiSerializer>(ns, name)
{
    /// <inheritdoc />
    protected override string ToLowLevel(GameStore value) => value.Value;

    /// <inheritdoc />
    protected override GameStore FromLowLevel(string value, AttributeResolver resolver) => GameStore.From(value);
}
