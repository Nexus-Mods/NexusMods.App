using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using NexusMods.Abstractions.Loadouts;
using NexusMods.App.UI.Controls.DataGrid;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.TxFunctions;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Pages.LoadoutGrid.Columns.ModEnabled;

public class ModEnabledViewModel : AViewModel<IModEnabledViewModel>, IModEnabledViewModel, IComparableColumn<LoadoutItemGroupId>
{
    private readonly IConnection _connection;

    [Reactive] public bool Enabled { get; set; } = false;

    public ReactiveCommand<bool, Unit> ToggleEnabledCommand { get; set; }

    [Reactive] public LoadoutItemGroupId Row { get; set; }

    public ModEnabledViewModel(IConnection conn)
    {
        _connection = conn;

        this.WhenActivated(d =>
        {
            this.WhenAnyValue(vm => vm.Row)
                .Select(groupId => LoadoutItemGroup.Observe(_connection, groupId))
                .Switch()
                .Select(group => !group.AsLoadoutItem().IsDisabled)
                .BindToVM(this, vm => vm.Enabled)
                .DisposeWith(d);
        });

        ToggleEnabledCommand = ReactiveCommand.CreateFromTask<bool, Unit>(async _ =>
        {
            using var tx = _connection.BeginTransaction();

            tx.Add(Row.Value, static (txInner, db, id) =>
            {
                var item = LoadoutItem.Load(db, id);
                if (item.IsDisabled)
                {
                    txInner.Retract(item.Id, LoadoutItem.Disabled, Null.Instance);
                }
                else
                {
                    txInner.Add(item.Id, LoadoutItem.Disabled, Null.Instance);
                }
            });

            await tx.Commit();
            return Unit.Default;
        });
    }

    public int Compare(LoadoutItemGroupId a, LoadoutItemGroupId b)
    {
        var db = _connection.Db;
        var aEnt = LoadoutItemGroup.Load(db, a);
        var bEnt = LoadoutItemGroup.Load(db, b);
        return aEnt.AsLoadoutItem().IsDisabled.CompareTo(bEnt.AsLoadoutItem().IsDisabled);
    }
}
