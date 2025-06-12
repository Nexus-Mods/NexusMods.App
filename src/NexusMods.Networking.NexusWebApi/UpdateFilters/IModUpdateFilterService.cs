using System.Reactive;
using NexusMods.Abstractions.NexusWebApi.Types.V2.Uid;

namespace NexusMods.Networking.NexusWebApi.UpdateFilters;

/// <summary>
/// Service responsible for managing filter triggers and providing filter notifications
/// for mod update observables.
/// </summary>
public interface IModUpdateFilterService : IDisposable
{
    /// <summary>
    /// Observable that triggers when filters should be re-evaluated.
    /// This is typically triggered when ignore file updates change or other filtering conditions change.
    /// </summary>
    IObservable<Unit> FilterTrigger { get; }
    
    /// <summary>
    /// Observe the hidden state of a specific file.
    /// </summary>
    /// <param name="fileUid">The unique identifier of the file to observe.</param>
    /// <returns>An observable that emits true when the file is hidden, false when it's visible.</returns>
    IObservable<bool> ObserveFileHiddenState(UidForFile fileUid);
    
    /// <summary>
    /// Manually trigger a filter re-evaluation.
    /// This is useful when external conditions that affect filtering have changed.
    /// </summary>
    void TriggerFilterUpdate();
    
    /// <summary>
    /// Hide (filter) a specific file from update notifications.
    /// </summary>
    /// <param name="fileUid">The unique identifier of the file to hide.</param>
    Task HideFileAsync(UidForFile fileUid);
    
    /// <summary>
    /// Hide (filter) multiple files from update notifications.
    /// </summary>
    /// <param name="fileUids">The unique identifiers of the files to hide.</param>
    Task HideFilesAsync(IEnumerable<UidForFile> fileUids);
    
    /// <summary>
    /// Show (unfilter) a specific file in update notifications.
    /// </summary>
    /// <param name="fileUid">The unique identifier of the file to show.</param>
    Task ShowFileAsync(UidForFile fileUid);
    
    /// <summary>
    /// Show (unfilter) multiple files in update notifications.
    /// </summary>
    /// <param name="fileUids">The unique identifiers of the files to show.</param>
    Task ShowFilesAsync(IEnumerable<UidForFile> fileUids);
}
