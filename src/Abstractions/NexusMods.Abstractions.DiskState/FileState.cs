using NexusMods.Abstractions.GameLocators;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;

namespace NexusMods.Abstractions.DiskState;

public record struct FileState(GamePath Path, DateTime LastModified, Size Size);

public record struct FileStateWithHash(FileState State, Hash Hash);
