using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts.Mods;

namespace NexusMods.Games.FOMOD;

public record EmptyDirectory : AModFile
{
    public required GamePath Directory { get; init; }
}
