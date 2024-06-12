using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Input;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Ids;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.App.UI.Controls.DataGrid;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Models;
using NexusMods.MnemonicDB.Abstractions.TxFunctions;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Pages.LoadoutGrid.Columns.ModEnabled;

public class ModEnabledViewModel : AViewModel<IModEnabledViewModel>, IModEnabledViewModel, IComparableColumn<ModId>
{
    private readonly IConnection _conn;

    [Reactive]
    public ModId Row { get; set; } = Initializers.ModId;

    [Reactive]
    public bool Enabled { get; set; } = false;

    [Reactive]
    public ModStatus Status { get; set; } = ModStatus.Installed;

    [Reactive]
    public ReactiveCommand<bool, Unit> ToggleEnabledCommand { get; set; }

    [Reactive]
    public ReactiveCommand<Unit, Unit> DeleteModCommand { get; set; }

    public ModEnabledViewModel(IConnection conn)
    {
        _conn = conn;

        this.WhenActivated(d =>
        {
            this.WhenAnyValue(vm => vm.Row)
                .SelectMany(id => Mod.Load(_conn.Db, id).Revisions())
                .Select(mod => mod.Enabled)
                .OnUI()
                .BindTo(this, vm => vm.Enabled)
                .DisposeWith(d);

            this.WhenAnyValue(vm => vm.Row)
                .SelectMany(id => Mod.Load(_conn.Db, id).Revisions())
                .Select(mod => mod.Status)
                .OnUI()
                .BindTo(this, vm => vm.Status)
                .DisposeWith(d);
        });
        ToggleEnabledCommand = ReactiveCommand.CreateFromTask<bool, Unit>(async enabled =>
        {
            using var tx = _conn.BeginTransaction();
            tx.Add(Row, static (txInner, db, id) =>
            {
                var mod = Mod.Load(db, id);
                txInner.Add(mod.Id, Mod.Enabled, !mod.Enabled);
            });
            await tx.Commit();
            return Unit.Default;
        });
        DeleteModCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            using var tx = _conn.BeginTransaction();
            tx.Delete(Row, true);
            await tx.Commit();
        });
    }

    public int Compare(ModId a, ModId b)
    {
        var db = _conn.Db;
        var aEnt = Mod.Load(db, a);
        var bEnt = Mod.Load(db, b);
        return (aEnt.Enabled).CompareTo(bEnt.Enabled);
    }
}
