using Bannerlord.LauncherManager;
using Bannerlord.LauncherManager.Models;
using NexusMods.DataModel.Games;

namespace NexusMods.Games.MountAndBlade2Bannerlord.Extensions
{
    public static class GameInstallationExtensions
    {
        public static string GetConfiguration(this GameInstallation gameInstallation, LauncherManagerFactory launcherManagerFactory)
        {
            var launcherManager = launcherManagerFactory.Get(gameInstallation);
            return launcherManager.GetPlatform() switch
            {
                GamePlatform.Win64 => Constants.Win64Configuration,
                GamePlatform.Xbox => Constants.XboxConfiguration,
                _ => string.Empty,
            };
        }
    }
}
