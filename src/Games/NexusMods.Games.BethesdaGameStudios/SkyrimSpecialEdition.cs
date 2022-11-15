using NexusMods.Interfaces;
using NexusMods.Interfaces.Components;

namespace NexusMods.Games.BethesdaGameStudios;

public class SkyrimSpecialEdition : IGame
{
    public string Name { get; }
    public string Slug { get; }
    public IEnumerable<GameInstallation> Installations { get; }
}