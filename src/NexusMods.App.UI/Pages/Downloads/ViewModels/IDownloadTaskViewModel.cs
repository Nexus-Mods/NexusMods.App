using System.Reactive;
using NexusMods.App.UI.Controls.Navigation;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Networking.Downloaders.Interfaces;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.Downloads.ViewModels;

/// <summary>
/// ViewModel for an individual download task displayed in the UI.
/// </summary>
public interface IDownloadTaskViewModel : IViewModelInterface
{
    
    public IDownloadTask DlTask { get; }
    
    /// <summary>
    /// e.g. 'My Cool Mod'
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// e.g. '1.0.0'
    /// </summary>
    public string Version { get; }

    /// <summary>
    /// e.g. 'Skyrim'
    /// </summary>
    public string Game { get; }
    
    /// <summary>
    /// Total size in humanized string format.
    /// </summary>
    public string HumanizedSize { get; }
    
    /// <summary>
    /// The DateTime when the download was completed in humanized string format.
    /// </summary>
    public string HumanizedCompletedTime { get; }

    /// <summary>
    /// e.g. 'Downloading'
    /// </summary>
    public DownloadTaskStatus Status { get; }

    /// <summary>
    /// e.g. '0'
    /// </summary>
    public long DownloadedBytes { get; }

    /// <summary>
    /// e.g. '1024'
    /// </summary>
    public long SizeBytes { get; }

    /// <summary>
    /// Current download speed of this task in bytes per second.
    /// </summary>
    public long Throughput { get; }
    
    /// <summary>
    /// Unique identifier for this task.
    /// </summary>
    public EntityId TaskId { get; }

    /// <summary>
    /// Whether this completed download was hidden from the UI (clear action).
    /// </summary>
    /// <value></value>
    public bool IsHidden { get; set; }

    /// <summary>
    /// Hides the task from the UI.
    /// Only works for completed tasks.
    /// </summary>
    public ReactiveCommand<Unit, Unit> HideCommand { get; }
    
    /// <summary>
    /// View the entry corresponding to this completed download in the mod Library page
    /// </summary>
    public ReactiveCommand<NavigationInformation, Unit> ViewInLibraryCommand { get; }

    /// <summary>
    /// Schedules a cancellation of the task.
    /// </summary>
    public Task Cancel();


    /// <summary>
    /// Suspends the task, keeping it around in memory.
    /// </summary>
    public Task Suspend();


    /// <summary>
    /// Resumes the task from a suspended state.
    /// </summary>
    public Task Resume();

}

