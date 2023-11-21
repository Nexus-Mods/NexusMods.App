using System.Diagnostics.CodeAnalysis;

namespace NexusMods.Games.AdvancedInstaller.UI.SelectLocation;

[ExcludeFromCodeCoverage]
internal class SelectLocationDesignViewModel : SelectLocationViewModel
{
    public SelectLocationDesignViewModel() : base(
        DesignTimeHelpers.CreateDesignGameLocationsRegister(),
        null,
        "Design Game Name") { }
}
