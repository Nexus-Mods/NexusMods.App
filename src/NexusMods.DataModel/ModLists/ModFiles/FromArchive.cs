using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;

namespace NexusMods.DataModel.ModLists.ModFiles;

public record FromArchive(GamePath To, HashRelativePath From) : AModFile(To);