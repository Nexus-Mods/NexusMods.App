using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using NexusMods.App.UI.Extensions;
using NexusMods.DataModel.Games;

namespace NexusMods.Games.AdvancedInstaller.UI.SelectLocation;

[ExcludeFromCodeCoverage]
internal class SelectLocationDesignViewModel : SelectLocationViewModel
{
    public SelectLocationDesignViewModel() : base(DesignTimeHelpers.CreateDesignGameLocationsRegister(), null,
        "Design Game Name") { }
}
