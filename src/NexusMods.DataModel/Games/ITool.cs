using NexusMods.DataModel.Loadouts;

namespace NexusMods.DataModel.Games;

/// <summary>
/// Specifies a tool that is run outside of the app. Could be the game itself,
/// a file generator, some sort of editor, patcher, etc.
/// </summary>
public interface ITool
{
    public IEnumerable<GameDomain> Domains { get; }

    public Task Execute(Loadout loadout);
    
    public string Name { get; }
}