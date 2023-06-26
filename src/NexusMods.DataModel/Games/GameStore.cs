using JetBrains.Annotations;
using Vogen;

namespace NexusMods.DataModel.Games;

/// <summary>
/// Represents a game store from which the game installation originates.
/// </summary>
[ValueObject<string>]
[Instance("Unknown", "Unknown")]
[Instance("Steam", "Steam")]
[Instance("GOG", "GOG")]
[Instance("EGS", "Epic Games Store")]
[Instance("Origin", "Origin")]
[Instance("EADesktop", "EA Desktop")]
[Instance("XboxGamePass", "Xbox Game Pass")]
[Instance("ManuallyAdded", "Manually Added")]
[PublicAPI]
public readonly partial struct GameStore { }
