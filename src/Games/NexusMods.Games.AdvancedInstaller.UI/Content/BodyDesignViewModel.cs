using NexusMods.DataModel.Games;
using NexusMods.DataModel.ModInstallers;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;
using NexusMods.Paths.FileTree;

namespace NexusMods.Games.AdvancedInstaller.UI;

public class BodyDesignViewModel : BodyViewModel
{
    public BodyDesignViewModel() : base(
        new DeploymentData(),
        "Design Mod Name",
        DesignTimeHelpers.CreateDesignFileTree(),
        DesignTimeHelpers.CreateDesignGameLocationsRegister(),
        null,
        "Design Game Name") { }
}
