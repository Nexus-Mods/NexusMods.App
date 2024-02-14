using System.Reactive.Disposables;
using System.Reactive.Linq;
using NexusMods.Abstractions.Loadouts.Mods;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Pages.LoadoutGrid.Columns.ModName;

public class ModNameDesignViewModel : AViewModel<IModNameViewModel>, IModNameViewModel
{
    public ModCursor Row { get; set; } = Initializers.ModCursor;

    [Reactive]
    public string Name { get; set; } = "";

    public ModNameDesignViewModel()
    {
        this.WhenActivated(d =>
        {
            this.WhenAnyValue(vm => vm.Row)
                .Select(row => $"Name for ({row.ModId})")
                .BindToUi(this, vm => vm.Name)
                .DisposeWith(d);
        });
    }
}
