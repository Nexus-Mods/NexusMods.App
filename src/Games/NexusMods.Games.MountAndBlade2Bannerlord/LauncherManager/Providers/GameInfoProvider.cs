using System.Diagnostics;
using Bannerlord.LauncherManager.External;
using NexusMods.DataModel;
using NexusMods.DataModel.Games;
using NexusMods.Games.MountAndBlade2Bannerlord.Services;

namespace NexusMods.Games.MountAndBlade2Bannerlord.LauncherManager.Providers;

internal sealed class GameInfoProvider : IGameInfoProvider
{
    private readonly GameInstallationContextAccessor _gameInstallationContextAccessor;

    public GameInfoProvider(GameInstallationContextAccessor gameInstallationContextAccessor)
    {
        _gameInstallationContextAccessor = gameInstallationContextAccessor;
    }

    public string GetInstallPath() => _gameInstallationContextAccessor.GetCurrent()?.InstallationPath.GetFullPath() ?? throw new UnreachableException();
}
