using System.Reactive;
using NexusMods.Abstractions.Jobs;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.UI;
using ReactiveUI;

namespace NexusMods.App.UI.LeftMenu.Items;

public interface ILaunchButtonViewModel : IViewModelInterface
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
    /// Returns an observable which signals whether the game is currently running.
    /// This signals the initials state immediately upon subscribing.
    /// </summary>
    public IObservable<bool> IsRunningObservable { get; }

    /// <summary>
    /// Text to display on the button.
    /// </summary>
    public string Label { get; }
    public Percent? Progress { get; }
}
