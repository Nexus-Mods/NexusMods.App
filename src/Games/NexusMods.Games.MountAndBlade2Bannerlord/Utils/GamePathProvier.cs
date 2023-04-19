using Bannerlord.LauncherManager;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;
using GameStore = NexusMods.DataModel.Games.GameStore;

namespace NexusMods.Games.MountAndBlade2Bannerlord.Utils;

public readonly struct GamePathProvier
{
    public static GamePathProvier FromStore(GameStore store) => new(store);

    private readonly string _configuration;

    private GamePathProvier(GameStore store)
    {
        _configuration = LauncherManagerHandler.GetConfigurationByPlatform(LauncherManagerHandler.FromStore(Converter.ToGameStoreTW(store)));
    }
    
    public GamePath PrimaryFile() => FromMainBin("TaleWorlds.MountAndBlade.Launcher.exe");
    public GamePath PrimaryXboxFile() => FromMainBin("Launcher.Native.exe");
    public GamePath PrimaryStandaloneFile() => FromMainBin(Constants.BannerlordExecutable);

    public GamePath BLSEStandaloneFile() => FromMainBin("Bannerlord.BLSE.Standalone.exe");
    public GamePath BLSELauncherFile() => FromMainBin("Bannerlord.BLSE.Launcher.exe");

    public GamePath BLSELauncherExFile() => FromMainBin("Bannerlord.BLSE.LauncherEx.exe");

    private GamePath FromMainBin(RelativePath toJoin) => new(GameFolderType.Game, Path.Combine("bin", _configuration).ToRelativePath().Join(toJoin));
}
