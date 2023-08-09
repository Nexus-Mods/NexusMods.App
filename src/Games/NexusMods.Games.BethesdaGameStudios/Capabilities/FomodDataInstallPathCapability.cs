using NexusMods.DataModel.Games.GameCapabilities.FomodCustomInstallPathCapability;
using NexusMods.Paths;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NexusMods.Games.BethesdaGameStudios.Capabilities
{
    internal class FomodDataInstallPathCapability : AFomodCustomInstallPathCapability
    {
        public override GamePath ModInstallationPath()
        {
            return new GamePath(GameFolderType.Game, new RelativePath("data"));
        }
    }
}
