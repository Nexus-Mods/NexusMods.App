using System.Windows.Input;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Abstractions.Ids;
using NexusMods.DataModel.Loadouts;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.RightContent.LoadoutGrid.Columns;

public class ModEnabledDesignViewModel : AViewModel<IModEnabledViewModel>, IModEnabledViewModel
{
    public IId Row { get; set; } = new Id64(EntityCategory.TestData, 1);

    [Reactive]
    public bool Enabled { get; set; }
    public ICommand ToggleEnabledCommand { get; }

    public ModEnabledDesignViewModel()
    {
        ToggleEnabledCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            // Bit of a delay to show simulate the roundtrip to the datastore
            await Task.Delay(1000);
            Enabled = !Enabled;
        });
    }
}
