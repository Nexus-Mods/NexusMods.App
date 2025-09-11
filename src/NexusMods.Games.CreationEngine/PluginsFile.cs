using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Synchronizers;
using NexusMods.Games.CreationEngine.Abstractions;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.Games.CreationEngine;

public class PluginsFile : IIntrinsicFile
{
    private readonly ICreationEngineGame _game;

    public PluginsFile(ICreationEngineGame game)
    {
        _game = game;
        Path = game.PluginsFile;
    }
    
    public GamePath Path { get; }
    
    public Task Write(Stream stream, Loadout.ReadOnly loadout, Dictionary<GamePath, SyncNode> syncTree, ITransaction tx)
    {
        throw new NotImplementedException();
    }

    public Task Ingest(Stream stream, Loadout.ReadOnly loadout, Dictionary<GamePath, SyncNode> syncTree, ITransaction tx)
    {
        throw new NotImplementedException();
    }
}
