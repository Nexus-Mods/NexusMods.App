using System.Reactive;
using NexusMods.App.UI.Overlays;
using ReactiveUI;

namespace NexusMods.App.UI.Windows;

public interface IMainWindowViewModel : IViewModelInterface, IWorkspaceWindow
{
    IOverlayViewModel? CurrentOverlay { get; }

    /// <summary>
    ///     This command is used to bring the window to front.
    /// </summary>
    /// <remarks>
    ///     Note(sewer)
    ///     Normally this would go into something like <see cref="IWorkspaceWindow"/>,
    ///     however there aren't enough use cases yet to justify this.
    /// </remarks>
    ReactiveCommand<Unit, Unit> ActivateWindowCommand { get; }
}
