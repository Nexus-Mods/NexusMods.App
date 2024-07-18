using NexusMods.Abstractions.Loadouts;
using NexusMods.MnemonicDB.Abstractions;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Controls.LoadoutBadge;

public class LoadoutBadgeViewModel : AViewModel<ILoadoutBadgeViewModel>, ILoadoutBadgeViewModel
{
 
    public LoadoutId? LoadoutId { get; set; }
    
    public LoadoutBadgeViewModel(IConnection conn)
    {
        
    }
    [Reactive] public string LoadoutShortName { get; private set; } = " ";
    [Reactive] public bool IsLoadoutSelected { get; set; } = false;
    [Reactive] public bool IsLoadoutApplied { get; private set; } = false;
    [Reactive] public bool IsLoadoutInProgress { get; private set; } = true;
}
