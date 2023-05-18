using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;

namespace NexusMods.DataModel.Loadouts.LoadoutSynchronizerDTOs;

public record FileMetaData(AbsolutePath Path, Hash Hash, Size Size);