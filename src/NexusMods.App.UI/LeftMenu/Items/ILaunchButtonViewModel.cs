using System.Reactive;
using NexusMods.Abstractions.Jobs;
using NexusMods.Abstractions.Loadouts;
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
    /// Returns an observable which signals whether the game is currently running.
    /// This signals the initials state immediately upon subscribing.
    /// </summary>
    public IObservable<bool> IsRunningObservable { get; }

    /// <summary>
    /// Text to display on the button.
    /// </summary>
    public string Label { get; }
    public Percent? Progress { get; }

    /// <summary>
    /// Refreshes initial state obtained during initialization of the view model.
    /// </summary>
    /// <remarks>
    ///     The Nexus App persists the left menu as part of workspaces in eternity,
    ///     meaning that spawning a new view will not re-evaluate existing state
    ///     if the workspace has already been created. Some state however needs to be
    ///     shared between workspaces, for example, if a given game is already running.
    ///
    ///     This achieves this in the meantime; however in the longer run, the code
    ///     around the 'launch' button and friends could be further refactored here.
    ///     Whether we're running games, and which games we're running should be considered
    ///     global state, per game, as opposed to ViewModel specific state.
    /// </remarks>
    public void Refresh();
}
