using NexusMods.Interfaces.Components;

namespace NexusMods.Interfaces.StoreLocatorTags;

public interface ISteamGame : IGame
{
    IEnumerable<int> SteamIds { get; }
}