using NexusMods.Abstractions.GameLocators;
using NexusMods.Cascade;
using NexusMods.Cascade.Patterns;
using NexusMods.Hashing.xxHash3;

namespace NexusMods.Abstractions.Games.FileHashes.Rows;

public readonly partial record struct LocatorIdPathHash(GameStore Store, LocatorId LocatorId, GamePath Path, Hash Hash) : IRowDefinition
{
}
