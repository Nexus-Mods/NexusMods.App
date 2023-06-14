using System.Reactive.Disposables;
using System.Reactive.Linq;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.Cursors;
using NexusMods.DataModel.Loadouts.Mods;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.RightContent.LoadoutGrid.Columns.ModName;

public class ModNameViewModel : AViewModel<IModNameViewModel>, IModNameViewModel, IComparableColumn<ModCursor>
{
    private readonly LoadoutRegistry _loadoutRegistry;

    [Reactive]
    public ModCursor Row { get; set; } = Initializers.ModCursor;

    [Reactive]
    public string Name { get; set; } = "";

    public ModNameViewModel(LoadoutRegistry loadoutRegistry, IDataStore store)
    {
        _loadoutRegistry = loadoutRegistry;
        this.WhenActivated(d =>
        {
            this.WhenAnyValue(vm => vm.Row)
                .SelectMany(loadoutRegistry.Revisions)
                .Select(id => store.Get<Mod>(id, true))
                .Select(m => m?.Name ?? "")
                .BindToUi(this, vm => vm.Name)
                .DisposeWith(d);

        });
    }

    public int Compare(ModCursor a, ModCursor b)
    {
        var aEnt = _loadoutRegistry.Get(a);
        var bEnt = _loadoutRegistry.Get(b);
        return string.Compare(aEnt?.Name ?? "", bEnt?.Name ?? "", StringComparison.Ordinal);
    }
}
