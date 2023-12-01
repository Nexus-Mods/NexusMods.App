using System.Diagnostics;
using Bannerlord.LauncherManager.External;
using NexusMods.DataModel.Games;

namespace NexusMods.Games.MountAndBlade2Bannerlord.LauncherManager.Providers;

internal sealed class GameInfoProvider : IGameInfoProvider
{
    private readonly GameInstallationContextAccessor _gameInstallationContextAccessor;

    public GameInfoProvider(GameInstallationContextAccessor gameInstallationContextAccessor)
    {
        _gameInstallationContextAccessor = gameInstallationContextAccessor;
    }

    public string GetInstallPath() => _gameInstallationContextAccessor.GameInstalltionContext?.InstallationPath.GetFullPath() ?? throw new UnreachableException();
}
