using System.Reactive.Disposables;
using System.Reactive.Linq;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.UI;
using NexusMods.MnemonicDB.Abstractions;
using ReactiveUI;

namespace NexusMods.App.UI.Controls.DataGrid.Columns;

public abstract class AColumnViewModel<TBaseInterface, TValue> : AViewModel<TBaseInterface>, IComparableColumn<LoadoutItemGroupId>
    where TBaseInterface : class, IViewModelInterface, ICellViewModel<TValue>
{
    private readonly IConnection _connection;

    protected AColumnViewModel(IConnection connection)
    {
        _connection = connection;

        this.WhenActivated(d =>
        {
            this.WhenAnyValue(vm => vm.Row)
                .Select(groupId => LoadoutItemGroup.Observe(_connection, groupId))
                .Switch()
                .Select(Selector)
                .OnUI()
                .BindTo(this, vm => vm.Value)
                .DisposeWith(d);
        });
        
    }

    /// <summary>
    /// A selector function to get the value of the column from the model
    /// </summary>
    protected abstract TValue Selector(LoadoutItemGroup.ReadOnly model);
    
    /// <summary>
    /// A comparer function to compare two values of the column
    /// </summary>
    protected abstract int Compare(TValue a, TValue b);

    private LoadoutItemGroupId _row;

    /// <summary>
    /// The Source ModId
    /// </summary>
    public LoadoutItemGroupId Row
    {
        get => _row;
        set => this.RaiseAndSetIfChanged(ref _row, value);
    }

    private TValue _value = default!;
    
    /// <summary>
    /// The Value of the Column
    /// </summary>
    public TValue Value
    {
        get => _value;
        set => this.RaiseAndSetIfChanged(ref _value, value);
    }

    public int Compare(LoadoutItemGroupId a, LoadoutItemGroupId b)
    {
        var db = _connection.Db;
        var aEnt = LoadoutItemGroup.Load(db, a);
        var bEnt = LoadoutItemGroup.Load(db, b);
        return Compare(Selector(aEnt), Selector(bEnt));
    }
}
