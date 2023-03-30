using System.Reactive.Disposables;
using System.Reactive.Linq;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.Cursors;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.RightContent.LoadoutGrid.Columns;

public class ModVersionViewModel : AViewModel<IModVersionViewModel>, IModVersionViewModel, IComparableColumn<ModCursor>
{
    private readonly LoadoutRegistry _loadoutRegistry;

    [Reactive]
    public ModCursor Row { get; set; }

    [Reactive] public string Version { get; set; } = "";

    public ModVersionViewModel(LoadoutRegistry loadoutRegistry, IDataStore store)
    {
        _loadoutRegistry = loadoutRegistry;
        this.WhenActivated(d =>
        {
            this.WhenAnyValue(vm => vm.Row)
                .SelectMany(loadoutRegistry.Revisions)
                .Select(id => store.Get<Mod>(id))
                .WhereNotNull()
                .Select(revision => revision.Version)
                .BindToUi(this, vm => vm.Version)
                .DisposeWith(d);
        });
    }

    public int Compare(ModCursor a, ModCursor b)
{
        var aEnt = _loadoutRegistry.Get(a);
        var bEnt = _loadoutRegistry.Get(b);
        return string.Compare(aEnt?.Version ?? "", bEnt?.Version ?? "", StringComparison.Ordinal);
    }
}
