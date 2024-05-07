using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Input;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Ids;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.App.UI.Controls.DataGrid;
using NexusMods.MnemonicDB.Abstractions;
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
                .SelectMany(id => _conn.Revisions(id))
                .Select(mod => mod.Enabled)
                .OnUI()
                .BindTo(this, vm => vm.Enabled)
                .DisposeWith(d);

            this.WhenAnyValue(vm => vm.Row)
                .SelectMany(id => _conn.Revisions(id))
                .Select(mod => mod.Status)
                .OnUI()
                .BindTo(this, vm => vm.Status)
                .DisposeWith(d);
        });
        ToggleEnabledCommand = ReactiveCommand.CreateFromTask<bool, Unit>(async enabled =>
        {
            var old = _conn.Db.Get(Row);
            await old.ToggleEnabled();
            return Unit.Default;
        });
        DeleteModCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            var mod = _conn.Db.Get(Row);
            await mod.Delete();
        });
    }

    public int Compare(ModId a, ModId b)
    {
        var db = _conn.Db;
        var aEnt = db.Get(a);
        var bEnt = db.Get(b);
        return (aEnt?.Enabled ?? false).CompareTo(bEnt?.Enabled ?? false);
    }
}
