using NexusMods.DataModel.Loadouts;
using NexusMods.Paths;

namespace NexusMods.Games.FOMOD;

public record EmptyDirectory : AModFile
{
    public required GamePath Directory { get; init; }
}
