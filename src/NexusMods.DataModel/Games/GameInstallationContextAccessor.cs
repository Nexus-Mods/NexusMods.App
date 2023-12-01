using NexusMods.Paths;

namespace NexusMods.DataModel.Games;

public sealed record GameInstallationContext(AbsolutePath InstallationPath, GameStore GameStore);

public sealed class GameInstallationContextAccessor
{
    private GameInstallationContext? _gameInstalltionCurrent;

    public GameInstallationContext? GetCurrent() => _gameInstalltionCurrent;

    public void SetCurrent(GameInstallationContext? gameInstallation) => _gameInstalltionCurrent = gameInstallation;
}
