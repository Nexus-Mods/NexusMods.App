using System.Reactive;
using NexusMods.Abstractions.NexusWebApi.Types.V2.Uid;

namespace NexusMods.Networking.NexusWebApi.UpdateFilters;

/// <summary>
/// This service provides services for filtering mod updates in the Nexus Mods App.
/// More specifically, this is the entry point for all filtering; it allows you to hide or show files
/// and unifies underlying filters with potential different under the hood implementations as one.
/// 
/// Likewise, this service also provides notifications for when filter rules are changed, and therefore
/// when mod updates should be re-evaluated for relevant callers.
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
    IObservable<bool> ObserveFileHiddenState(FileUid fileUid);
    
    /// <summary>
    /// Get the current hidden state of a specific file synchronously.
    /// </summary>
    /// <param name="fileUid">The unique identifier of the file to check.</param>
    /// <returns>True if the file is currently hidden, false if it's visible.</returns>
    bool IsFileHidden(FileUid fileUid);
    
    /// <summary>
    /// Manually trigger a filter re-evaluation.
    /// This is useful when external conditions that affect filtering have changed.
    /// </summary>
    void TriggerFilterUpdate();
    
    /// <summary>
    /// Hide (filter) a specific file from update notifications.
    /// </summary>
    /// <param name="fileUid">The unique identifier of the file to hide.</param>
    Task HideFileAsync(FileUid fileUid);
    
    /// <summary>
    /// Hide (filter) multiple files from update notifications.
    /// </summary>
    /// <param name="fileUids">The unique identifiers of the files to hide.</param>
    Task HideFilesAsync(IEnumerable<FileUid> fileUids);
    
    /// <summary>
    /// Show (unfilter) a specific file in update notifications.
    /// </summary>
    /// <param name="fileUid">The unique identifier of the file to show.</param>
    Task ShowFileAsync(FileUid fileUid);
    
    /// <summary>
    /// Show (unfilter) multiple files in update notifications.
    /// </summary>
    /// <param name="fileUids">The unique identifiers of the files to show.</param>
    Task ShowFilesAsync(IEnumerable<FileUid> fileUids);
    
    /// <summary>
    /// Filters a mod update to hide ignored files. Returns null if all files are filtered out.
    /// This method automatically applies all filters which are enabled by default.
    /// </summary>
    /// <param name="modUpdateOnPage">The mod update to filter.</param>
    /// <returns>The filtered mod update, or null if all files should be hidden.</returns>
    ModUpdateOnPage? SelectMod(ModUpdateOnPage modUpdateOnPage);
    
    /// <summary>
    /// Filters a mod page update to hide ignored files. Returns null if all file mappings are filtered out.
    /// This method automatically applies all filters which are enabled by default.
    /// </summary>
    /// <param name="modUpdatesOnModPage">The mod page update to filter.</param>
    /// <returns>The filtered mod page update, or null if all file mappings should be hidden.</returns>
    ModUpdatesOnModPage? SelectModPage(ModUpdatesOnModPage modUpdatesOnModPage);
}
