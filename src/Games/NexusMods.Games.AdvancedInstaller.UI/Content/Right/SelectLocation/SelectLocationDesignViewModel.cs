using System.Diagnostics.CodeAnalysis;

namespace NexusMods.Games.AdvancedInstaller.UI.SelectLocation;

[ExcludeFromCodeCoverage]
internal class SelectLocationDesignViewModel() : SelectLocationViewModel(
    DesignTimeHelpers.CreateDesignGameLocationsRegister(),
    null,
    "Design Game Name");
