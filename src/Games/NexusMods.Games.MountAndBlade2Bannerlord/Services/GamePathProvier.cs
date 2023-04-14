using Bannerlord.LauncherManager;
using Bannerlord.LauncherManager.Models;
using NexusMods.DataModel.Games;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;

namespace NexusMods.Games.MountAndBlade2Bannerlord.Services;

public sealed class GamePathProvier
{
    public AbsolutePath PrimaryFile => FromMainBin("TaleWorlds.MountAndBlade.Launcher.exe").CombineChecked(_installation.Locations[GameFolderType.Game]);
    public AbsolutePath PrimaryXboxFile => FromMainBin("Launcher.Native.exe").CombineChecked(_installation.Locations[GameFolderType.Game]);
    public AbsolutePath PrimaryStandaloneFile => FromMainBin(Constants.BannerlordExecutable).CombineChecked(_installation.Locations[GameFolderType.Game]);

    public AbsolutePath BLSEStandaloneFile => FromMainBin(Constants.BLSEExecutable).CombineChecked(_installation.Locations[GameFolderType.Game]);
    public AbsolutePath BLSELauncherFile => FromMainBin("Bannerlord.BLSE.Launcher.exe").CombineChecked(_installation.Locations[GameFolderType.Game]);

    public AbsolutePath BLSELauncherExFile => FromMainBin("Bannerlord.BLSE.LauncherEx.exe").CombineChecked(_installation.Locations[GameFolderType.Game]);

    private readonly LauncherManagerFactory _launcherManagerFactory;
    private readonly GameInstallation _installation;

    public GamePathProvier(LauncherManagerFactory launcherManagerFactory, GameInstallation installation)
    {
        _launcherManagerFactory = launcherManagerFactory;
        _installation = installation;
    }

    private string GetConfiguration()
    {
        var launcherManager = _launcherManagerFactory.Get(_installation);
        return launcherManager.GetPlatform() switch
        {
            GamePlatform.Win64 => Constants.Win64Configuration,
            GamePlatform.Xbox => Constants.XboxConfiguration,
            _ => string.Empty,
        };
    }
    private GamePath FromMainBin(RelativePath toJoin) => new(GameFolderType.Game, Path.Combine("bin", $"{GetConfiguration()}").ToRelativePath().Join(toJoin));
}
