using System.Reactive.Disposables;
using System.Reactive.Linq;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.Cursors;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.RightContent.LoadoutGrid.Columns;

public class ModNameViewModel : AViewModel<IModNameViewModel>, IModNameViewModel
{
    public ModCursor Row { get; set; } = Initializers.ModCursor;

    [Reactive]
    public string Name { get; set; } = "";

    public ModNameViewModel(LoadoutRegistry registry)
    {
        this.WhenActivated(d =>
        {
            this.WhenAnyValue(vm => vm.Row)
                .Select(registry.Get)
                .Select(m => m?.Name ?? "")
                .BindToUi(this, vm => vm.Name)
                .DisposeWith(d);
        });
    }
}
