using System.Reactive;
using NexusMods.Abstractions.Activities;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Ids;
using ReactiveUI;

namespace NexusMods.App.UI.LeftMenu.Items;

public interface ILaunchButtonViewModel : ILeftMenuItemViewModel
{
    /// <summary>
    /// The currently selected loadout.
    /// </summary>
    public LoadoutId LoadoutId { get; set; }

    /// <summary>
    /// The command to execute when the button is clicked.
    /// </summary>
    public ReactiveCommand<Unit, Unit> Command { get; set; }

    /// <summary>
    /// Text to display on the button.
    /// </summary>
    public string Label { get; }
    public Percent? Progress { get; }

}
