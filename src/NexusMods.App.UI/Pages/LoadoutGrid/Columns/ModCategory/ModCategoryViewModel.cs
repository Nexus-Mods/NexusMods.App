using System.Reactive.Disposables;
using System.Reactive.Linq;
using DynamicData;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.App.UI.Controls.DataGrid;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Query;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Pages.LoadoutGrid.Columns.ModCategory;

public class ModCategoryViewModel : AViewModel<IModCategoryViewModel>, IModCategoryViewModel, IComparableColumn<ModId>
{
    private readonly IConnection _conn;
    [Reactive] public ModId Row { get; set; } = Initializers.ModId;

    [Reactive] public string Category { get; set; } = "";

    public ModCategoryViewModel(IConnection conn)
    {
        _conn = conn;
        this.WhenActivated(d =>
        {
            this.WhenAnyValue(vm => vm.Row)
                .SelectMany(id => conn.ObserveDatoms(SliceDescriptor.Create(id, Mod.Revision.GetDbId(conn.Registry.Id), conn.Registry)))
                .QueryWhenChanged(f => Mod.Load(conn.Db, f.First().E))
                .Select(revision => revision.Category)
                .OnUI()
                .BindTo(this, vm => vm.Category)
                .DisposeWith(d);
        });
    }

    public int Compare(ModId a, ModId b)
    {
        var db = _conn.Db;
        var aMod = Mod.Load(db, a);
        var bMod = Mod.Load(db, b);
        return string.Compare(aMod.Category.ToString() ?? "", bMod.Category.ToString() ?? "", StringComparison.Ordinal);
    }
}
