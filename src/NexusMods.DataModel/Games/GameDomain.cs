using Vogen;

namespace NexusMods.DataModel.Games;

/// <summary>
/// Machine friendly name for the game, should be devoid of special characters
/// that may conflict with URLs or file paths.
///
/// Sometimes used as a Game ID.
/// </summary>
/// <remarks>
///    Usually we match these with NexusMods' URLs.
/// </remarks>
[ValueObject<string>]
[Instance("Cyberpunk2077", "cyberpunk2077")]
// ReSharper disable once PartialTypeWithSinglePart
public partial struct GameDomain { }
