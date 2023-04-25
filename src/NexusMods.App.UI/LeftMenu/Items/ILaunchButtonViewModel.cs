using System.Reactive;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.RateLimiting;
using ReactiveUI;

namespace NexusMods.App.UI.LeftMenu.Items;

public interface ILaunchButtonViewModel : ILeftMenuItemViewModel
{
    /// <summary>
    /// The game to launch. TODO: this should be a game installation
    /// long term, but the rest of the app is not ready for that yet.
    /// </summary>
    public IGame Game { get; set; }

    /// <summary>
    /// The command to execute when the button is clicked.
    /// </summary>
    public ReactiveCommand<Unit, Unit> Command { get; set; }

    /// <summary>
    /// True if the button is enabled
    /// </summary>
    public bool IsEnabled { get; }

    /// <summary>
    /// True if the app is processing something so the button should
    /// be replaced with a progress bar
    /// </summary>
    public bool IsRunning { get; }

    /// <summary>
    /// Text to display on the button.
    /// </summary>
    public string Label { get; }
    public Percent? Progress { get; }

}
