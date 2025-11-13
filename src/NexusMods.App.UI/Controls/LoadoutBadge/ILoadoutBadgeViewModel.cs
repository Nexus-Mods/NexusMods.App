using DynamicData.Kernel;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Sdk.Loadouts;
using NexusMods.UI.Sdk;

namespace NexusMods.App.UI.Controls.LoadoutBadge;

public interface ILoadoutBadgeViewModel : IViewModelInterface
{
    Optional<Loadout.ReadOnly> LoadoutValue { get; set;  }
    
    string LoadoutShortName { get; }
    
    bool IsLoadoutSelected { get; set; }
    
    bool IsLoadoutApplied { get; }
    
    bool IsLoadoutInProgress { get; }
    
    bool IsVisible { get; }
}
