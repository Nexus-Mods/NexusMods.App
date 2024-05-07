using System.Reactive.Disposables;
using System.Reactive.Linq;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Ids;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.Abstractions.Serialization;
using NexusMods.App.UI.Controls.DataGrid;
using NexusMods.MnemonicDB.Abstractions;
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
                .SelectMany(id => conn.Revisions(id))
                .WhereNotNull()
                .Select(revision => revision.Category)
                .OnUI()
                .BindTo(this, vm => vm.Category)
                .DisposeWith(d);
        });
    }

    public int Compare(ModId a, ModId b)
    {
        var db = _conn.Db;
        var aMod = _conn.Db.Get(a);
        var bMod = _conn.Db.Get(b);
        return string.Compare(aMod?.Category.ToString() ?? "", bMod?.Category.ToString() ?? "", StringComparison.Ordinal);
    }
}
