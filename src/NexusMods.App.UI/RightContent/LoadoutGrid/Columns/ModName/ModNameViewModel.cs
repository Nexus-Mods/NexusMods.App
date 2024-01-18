using System.Reactive.Disposables;
using System.Reactive.Linq;
using NexusMods.Abstractions.DataModel.Entities.Mods;
using NexusMods.Abstractions.Games.DTO;
using NexusMods.Abstractions.Serialization;
using NexusMods.App.UI.Controls.DataGrid;
using NexusMods.DataModel.Loadouts;
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
