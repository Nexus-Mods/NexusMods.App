using System.Reactive.Disposables;
using System.Reactive.Linq;
using NexusMods.Abstractions.DataModel.Entities.Mods;
using NexusMods.Abstractions.Games.DTO;
using NexusMods.Abstractions.Games.Loadouts;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.App.UI.Controls.DataGrid;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.RightContent.LoadoutGrid.Columns.ModInstalled;

public class ModInstalledViewModel : AViewModel<IModInstalledViewModel>, IModInstalledViewModel, IComparableColumn<ModCursor>
{
    private readonly ILoadoutRegistry _loadoutRegistry;

    [Reactive]
    public ModCursor Row { get; set; }

    [Reactive]
    public DateTime Installed { get; set; }

    [Reactive]
    public ModStatus Status { get; set; }

    public ModInstalledViewModel(ILoadoutRegistry loadoutRegistry)
    {
        _loadoutRegistry = loadoutRegistry;
        this.WhenActivated(d =>
        {
            this.WhenAnyValue(vm => vm.Row)
                .SelectMany(loadoutRegistry.RevisionsAsMods)
                .Select(mod => mod.Installed)
                .BindToUi(this, vm => vm.Installed)
                .DisposeWith(d);

            this.WhenAnyValue(vm => vm.Row)
                .SelectMany(loadoutRegistry.RevisionsAsMods)
                .Select(mod => mod.Status)
                .BindToUi(this, vm => vm.Status)
                .DisposeWith(d);
        });
    }

    public int Compare(ModCursor a, ModCursor b)
    {
        var aEnt = _loadoutRegistry.Get(a);
        var bEnt = _loadoutRegistry.Get(b);
        return aEnt?.Installed.CompareTo(bEnt?.Installed) ?? 0;
    }
}
