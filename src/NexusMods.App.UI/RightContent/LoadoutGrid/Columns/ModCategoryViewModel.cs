using System.Reactive.Disposables;
using System.Reactive.Linq;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.Cursors;
using NexusMods.DataModel.Loadouts.Mods;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
namespace NexusMods.App.UI.RightContent.LoadoutGrid.Columns;

public class ModCategoryViewModel : AViewModel<IModCategoryViewModel>, IModCategoryViewModel, IComparableColumn<ModCursor>
{
    private readonly LoadoutRegistry _loadoutRegistry;
    [Reactive] public ModCursor Row { get; set; } = Initializers.ModCursor;

    [Reactive] public string Category { get; set; } = "";

    public ModCategoryViewModel(LoadoutRegistry loadoutRegistry, IDataStore store)
    {
        _loadoutRegistry = loadoutRegistry;

        this.WhenActivated(d =>
        {
            this.WhenAnyValue(vm => vm.Row)
                .SelectMany(loadoutRegistry.Revisions)
                .Select(id => store.Get<Mod>(id, true))
                .WhereNotNull()
                .Select(revision => revision.ModCategory)
                .BindToUi(this, vm => vm.Category)
                .DisposeWith(d);
        });
    }

    public int Compare(ModCursor a, ModCursor b)
    {
        var aEnt = _loadoutRegistry.Get(a);
        var bEnt = _loadoutRegistry.Get(b);
        return string.Compare(aEnt?.ModCategory ?? "", bEnt?.ModCategory ?? "", StringComparison.Ordinal);
    }
}
