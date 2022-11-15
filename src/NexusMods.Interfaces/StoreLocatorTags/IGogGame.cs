using NexusMods.Interfaces.Components;

namespace NexusMods.Interfaces.StoreLocatorTags;

public interface IGogGame : IGame
{
    IEnumerable<long> GogIds { get; }

}