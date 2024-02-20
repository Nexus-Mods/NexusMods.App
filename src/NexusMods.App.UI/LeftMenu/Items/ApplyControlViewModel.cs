using System.Windows.Input;
using NexusMods.Abstractions.Loadouts;
using ReactiveUI;

namespace NexusMods.App.UI.LeftMenu.Items;

public class ApplyControlViewModel : AViewModel<IApplyControlViewModel>, IApplyControlViewModel
{
    public ICommand ApplyCommand { get; }

    private readonly LoadoutId _loadoutId;

    public ApplyControlViewModel(LoadoutId loadoutId, ILoadoutRegistry loadoutService)
    {
        _loadoutId = loadoutId;
        var loadout = loadoutService.Get(loadoutId);
        ApplyCommand = ReactiveCommand.Create(() => { });
    }
}
