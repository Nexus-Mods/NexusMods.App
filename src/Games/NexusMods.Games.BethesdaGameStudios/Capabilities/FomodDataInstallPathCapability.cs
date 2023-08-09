using NexusMods.DataModel.Games.GameCapabilities.FomodCustomInstallPathCapability;
using NexusMods.Paths;

namespace NexusMods.Games.BethesdaGameStudios.Capabilities
{

    /// <summary>
    /// Capability to install FOMODs to the Data folder instead of the GameRoot.
    /// </summary>
    public class FomodDataInstallPathCapability : AFomodCustomInstallPathCapability
    {
        public override GamePath ModInstallationPath()
        {
            return new GamePath(GameFolderType.Game, new RelativePath("data"));
        }
    }
}
