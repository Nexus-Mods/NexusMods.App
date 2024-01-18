using NexusMods.Abstractions.DataModel.Entities.Mods;
using NexusMods.Abstractions.Installers.DTO;

namespace NexusMods.Games.FOMOD;

public record EmptyDirectory : AModFile
{
    public required GamePath Directory { get; init; }
}
