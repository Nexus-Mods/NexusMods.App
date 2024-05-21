using System.Reactive;
using Humanizer.Bytes;
using NexusMods.App.UI.Controls.Navigation;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Networking.Downloaders.Interfaces;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.Downloads.ViewModels;

public class DownloadTaskDesignViewModel : AViewModel<IDownloadTaskViewModel>, IDownloadTaskViewModel
{
    public IDownloadTask DlTask => null!;
    public string Name { get; set; } = "Design Mod";
    public string Version { get; set; } = "1.0.0";
    public string Game { get; set; } = "Unknown Game";
    public string HumanizedSize => ByteSize.FromBytes(SizeBytes).ToString();
    public string HumanizedCompletedTime { get; } = "-";
    public DownloadTaskStatus Status { get; set; } = DownloadTaskStatus.Idle;
    public EntityId TaskId { get; set; } = EntityId.From(1024);

    public bool IsHidden { get; set; } = false;

    public ReactiveCommand<Unit, Unit> HideCommand { get; } = ReactiveCommand.Create(() => { });
    public ReactiveCommand<NavigationInformation, Unit> ViewInLibraryCommand { get; } = ReactiveCommand.Create<NavigationInformation>(_ => { });
    public Task Cancel() => Task.CompletedTask;
    public Task Suspend() => Task.CompletedTask;
    public Task Resume() => Task.CompletedTask;

    public long DownloadedBytes { get; set; } = 1024 * 1024 * 512;
    public long SizeBytes { get; set; } = 1024 * 1024 * 1337;
    public long Throughput { get; set; } = 0;
}
