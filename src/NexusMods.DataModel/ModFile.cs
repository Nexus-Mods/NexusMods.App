using NexusMods.DataModel.Abstractions;
using NexusMods.Paths;

namespace NexusMods.DataModel;

public record ModFile(GamePath To) : Entity
{
}