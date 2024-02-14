using JetBrains.Annotations;
using NexusMods.App.UI.Pages.Downloads.ViewModels;

namespace NexusMods.App.UI.Overlays;

/// <summary>
/// Interface for managing and triggering various overlays that can happen within the application.
/// </summary>
public interface IOverlayController
{
    /// <summary>
    /// Tells you when to apply the next overlay to be shown to the screen.
    /// </summary>
    IObservable<SetOverlayItem?> ApplyNextOverlay { get; }

    /// <summary>
    /// Retrieves the last overlay ViewModel broadcasted via <see cref="ApplyNextOverlay"/>.
    /// </summary>
    /// <returns>The ViewModel.</returns>
    SetOverlayItem? GetLastOverlay();

    /// <summary>
    /// Shows the overlay responsible for user being asked if they want to cancel one or more downloads.
    /// </summary>
    /// <param name="tasks">The download tasks to cancel.</param>
    /// <param name="viewItem">
    ///     Any Avalonia control/item.
    ///     This is used to determine which window to spawn the overlay in.
    /// </param>
    public Task<bool> ShowCancelDownloadOverlay(IReadOnlyList<IDownloadTaskViewModel> tasks, object? viewItem = null);

    /// <summary>
    /// Sets the current overlay.
    /// </summary>
    /// <param name="item">The item to be shown.</param>
    /// <param name="tcs">This is signaled 'true' once the overlay has been dismissed.</param>
    public void SetOverlayContent(SetOverlayItem item, TaskCompletionSource<bool>? tcs = null);
}

/// <summary>
/// Item in the overlay system.
/// </summary>
[PublicAPI]
public struct SetOverlayItem
{
    // Note: Needs to be property for Reactive binding. Do not refactor to field (this includes use of Records).

    /// <summary>
    /// The ViewModel to be displayed.
    /// </summary>
    public IOverlayViewModel VM { get; }

    /// <summary>
    /// Any Avalonia control/item.
    /// This is used to determine which window to spawn the overlay in.
    /// </summary>
    public object? ViewItem { get; }

    public SetOverlayItem(IOverlayViewModel vm)
    {
        VM = vm;
    }

    public SetOverlayItem(IOverlayViewModel vm, object? viewItem)
    {
        VM = vm;
        ViewItem = viewItem;
    }
}
