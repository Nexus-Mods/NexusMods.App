using System.Windows.Input;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Abstractions.Ids;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.Cursors;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.RightContent.LoadoutGrid.Columns;

public class ModEnabledDesignViewModel : AViewModel<IModEnabledViewModel>, IModEnabledViewModel
{
    public ModCursor Row { get; set; } = Initializers.ModCursor;

    [Reactive]
    public bool Enabled { get; set; }
    public ICommand ToggleEnabledCommand { get; }

    public ModEnabledDesignViewModel()
    {
        ToggleEnabledCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            // Bit of a delay to show simulate the roundtrip to the datastore
            await Task.Delay(2000);
            Enabled = !Enabled;
        });
    }
}
