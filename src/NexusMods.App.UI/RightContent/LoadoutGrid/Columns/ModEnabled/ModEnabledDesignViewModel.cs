using System.Reactive;
using System.Windows.Input;
using NexusMods.Abstractions.Loadouts.Mods;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.RightContent.LoadoutGrid.Columns.ModEnabled;

public class ModEnabledDesignViewModel : AViewModel<IModEnabledViewModel>, IModEnabledViewModel
{
    public ModCursor Row { get; set; } = Initializers.ModCursor;

    [Reactive]
    public bool Enabled { get; set; }

    [Reactive] public ModStatus Status { get; set; } = ModStatus.Failed;
    public ICommand ToggleEnabledCommand { get; }
    public ICommand DeleteModCommand { get; }

    public ModEnabledDesignViewModel()
    {
        ToggleEnabledCommand = ReactiveCommand.CreateFromTask<bool, Unit>(async state =>
        {
            // Bit of a delay to show simulate the roundtrip to the datastore
            await Task.Delay(2000);
            Enabled = state;
            return Unit.Default;
        });

        DeleteModCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            Status = ModStatus.Installing;
            await Task.Delay(2000);
            Status = ModStatus.Installed;
        });
    }
}
