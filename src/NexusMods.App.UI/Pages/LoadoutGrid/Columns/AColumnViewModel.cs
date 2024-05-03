using System.Linq.Expressions;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Ids;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.App.UI.Controls.DataGrid;
using NexusMods.MnemonicDB.Abstractions;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Pages.LoadoutGrid.Columns;

public abstract class AColumnViewModel<TBaseInterface, TValue> : AViewModel<TBaseInterface>, IComparableColumn<ModId>
    where TBaseInterface : class, IViewModelInterface
{
    private readonly IConnection _conn;

    protected AColumnViewModel(IConnection conn, Expression<Func<Mod.Model, TValue?>> selector)
    {
        _conn = conn;
        
        this.WhenActivated(d =>
        {
            this.WhenAnyValue(vm => vm.Row)
                .SelectMany(id => _conn.Revisions(id))
                .Select(Selector)
                .OnUI()
                .BindTo(this, vm => vm.Value)
                .DisposeWith(d);
        });
        
    }

    /// <summary>
    /// A selector function to get the value of the column from the model
    /// </summary>
    protected abstract TValue Selector(Mod.Model model);

    protected abstract TValue Binder();

    /// <summary>
    /// A comparer function to compare two values of the column
    /// </summary>
    protected abstract int Compare(TValue a, TValue b);
    
    /// <summary>
    /// The Source ModId
    /// </summary>
    [Reactive] public ModId Row { get; set; }
    
    /// <summary>
    /// The Value of the Column
    /// </summary>
    [Reactive] public TValue Value { get; set; } = default!;
    
    public int Compare(ModId a, ModId b)
    {
        var db = _conn.Db;
        var aEnt = db.Get(a);
        var bEnt = db.Get(b);
        return Compare(Selector(aEnt), Selector(bEnt));
    }
}
