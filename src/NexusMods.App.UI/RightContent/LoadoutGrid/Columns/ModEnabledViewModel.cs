using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Input;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Abstractions.Ids;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.Cursors;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.RightContent.LoadoutGrid.Columns;

public class ModEnabledViewModel : AViewModel<IModEnabledViewModel>, IModEnabledViewModel
{
    public ModCursor Row { get; set; } = Initializers.ModCursor;

    [Reactive]
    public bool Enabled { get; set; } = false;

    [Reactive]
    public ICommand ToggleEnabledCommand { get; set; }

    public ModEnabledViewModel(LoadoutRegistry loadoutRegistry, IDataStore store)
    {
        this.WhenActivated(d =>
        {
            this.WhenAnyValue(vm => vm.Row)
                .SelectMany(cursor => loadoutRegistry.Revisions(cursor.LoadoutId))
                .Select(id => store.Get<Mod>(id))
                .WhereNotNull()
                .Select(mod => mod.Enabled)
                .BindToUi(this, vm => vm.Enabled)
                .DisposeWith(d);
        });
        ToggleEnabledCommand = ReactiveCommand.Create(() =>
        {
            var mod = loadoutRegistry.Get(Row);
            if (mod is null) return;

            var oldState = mod.Enabled ? "Enabled" : "Disabled";
            var newState = !mod.Enabled ? "Enabled" : "Disabled";

            loadoutRegistry.Alter(Row,
                $"Setting {mod.Name} from {oldState} to {newState}",
                mod => mod! with { Enabled = !mod?.Enabled ?? false });
        });
    }
}
