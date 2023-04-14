using System.Runtime.CompilerServices;
using NexusMods.DataModel.Games;

namespace NexusMods.Games.MountAndBlade2Bannerlord.Services;

public sealed class GamePathProvierFactory
{
    private readonly LauncherManagerFactory _launcherManagerFactory;
    private readonly ConditionalWeakTable<GameInstallation, GamePathProvier> _instances = new();

    public GamePathProvierFactory(LauncherManagerFactory launcherManagerFactory)
    {
        _launcherManagerFactory = launcherManagerFactory;

    }

    public GamePathProvier Create(GameInstallation installation)
    {
        return _instances.GetValue(installation, x => new GamePathProvier(_launcherManagerFactory, installation));
    }
}
