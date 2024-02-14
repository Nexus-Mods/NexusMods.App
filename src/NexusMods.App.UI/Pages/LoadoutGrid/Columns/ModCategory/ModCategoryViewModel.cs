using System.Reactive.Disposables;
using System.Reactive.Linq;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.Abstractions.Serialization;
using NexusMods.App.UI.Controls.DataGrid;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Pages.LoadoutGrid.Columns.ModCategory;

public class ModCategoryViewModel : AViewModel<IModCategoryViewModel>, IModCategoryViewModel, IComparableColumn<ModCursor>
{
    private readonly ILoadoutRegistry _loadoutRegistry;
    [Reactive] public ModCursor Row { get; set; } = Initializers.ModCursor;

    [Reactive] public string Category { get; set; } = "";

    public ModCategoryViewModel(ILoadoutRegistry loadoutRegistry, IDataStore store)
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
