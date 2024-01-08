using NexusMods.DataModel.Games;
using NexusMods.DataModel.Loadouts;

namespace NexusMods.Games.FOMOD;

public record EmptyDirectory : AModFile
{
    public required GamePath Directory { get; init; }
}
