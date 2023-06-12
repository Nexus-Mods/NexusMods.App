using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.JsonConverters;
using NexusMods.Paths;

namespace NexusMods.DataModel.Games.ManuallyAdded;

[JsonName("ManuallyAddedGame")]
public record ManuallyAddedGame : Entity, IGameLocatorResultMetadata
{
    public override EntityCategory Category => EntityCategory.ManuallyAddedGame;

    public required GameDomain GameDomain { get; init; }

    public required Version Version { get; init; }

    public required AbsolutePath Path { get; init; }
}
