using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games.DTO;
using NexusMods.Abstractions.MultiFn;
using NexusMods.Hashing.xxHash64;

using File = NexusMods.Abstractions.Loadouts.Files.File;

namespace NexusMods.Abstractions.Loadouts.Synchronizers;

public class WriteGeneratedFileFn : AMultiFn<(File.Model File, Stream stream, Loadout.Model Loadout, FlattenedLoadout FlattenedLoadout, FileTree FileTree), Hash?>
{
    public static WriteGeneratedFileFn Instance { get; } = new();
    
}
