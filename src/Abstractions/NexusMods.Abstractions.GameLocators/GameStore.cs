using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
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
