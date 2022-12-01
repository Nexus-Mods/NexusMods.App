using NexusMods.Hashing.xxHash64;
using NexusMods.Interfaces;
using NexusMods.Paths;

namespace NexusMods.DataModel.ModLists.ModFiles;

public record GameFile(GameInstallation Installation, Hash Hash, GamePath To, Size Size) : AModFile(To);