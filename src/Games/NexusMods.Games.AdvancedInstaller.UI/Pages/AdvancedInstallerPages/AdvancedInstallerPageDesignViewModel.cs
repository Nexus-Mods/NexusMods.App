using System.Diagnostics.CodeAnalysis;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.ModInstallers;
using NexusMods.Paths;
using NexusMods.Paths.FileTree;

namespace NexusMods.Games.AdvancedInstaller.UI;

[ExcludeFromCodeCoverage]
// ReSharper disable once UnusedType.Global
internal class AdvancedInstallerPageDesignViewModel : AdvancedInstallerPageViewModel
{
    public AdvancedInstallerPageDesignViewModel() : base(
        "Design Mod Name",
        DesignTimeHelpers.CreateDesignFileTree(),
        DesignTimeHelpers.CreateDesignGameLocationsRegister(),
        "Design Game Name") { }
}
