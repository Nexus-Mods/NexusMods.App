using System.Reactive.Disposables;
using System.Reactive.Linq;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.Cursors;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.RightContent.LoadoutGrid.Columns;

public class ModNameViewModel : AViewModel<IModNameViewModel>, IModNameViewModel
{
    [Reactive]
    public ModCursor Row { get; set; } = Initializers.ModCursor;

    [Reactive]
    public string Name { get; set; } = "";

    public ModNameViewModel(LoadoutRegistry registry, IDataStore store)
    {
        this.WhenActivated(d =>
        {
            this.WhenAnyValue(vm => vm.Row)
                .SelectMany(registry.Revisions)
                .Select(id => store.Get<Mod>(id))
                .Select(m => m?.Name ?? "")
                .BindToUi(this, vm => vm.Name)
                .DisposeWith(d);
        });
    }
}
