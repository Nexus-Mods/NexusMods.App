using DynamicData;
using NexusMods.App.UI.Pages.Downloads.ViewModels;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Networking.Downloaders.Interfaces;

namespace NexusMods.App.UI.Pages.Downloads;

public class InProgressDesignViewModel : InProgressViewModel
{
    public InProgressDesignViewModel()
    {
        // Note (Al12rs): We can't simply assign a new collection to the Tasks property,
        // because all the bindings are already subscribed to the old collection.
        // It would be possible to unsubscribe from the old collection and subscribe to the new one,
        // but that would make all the bindings code much more messy, with nested subscriptions.
        // Instead, we add items to an existing collection initialized in the parent VM, so bindings are maintained.

        DesignTimeDownloadTasks.AddOrUpdate(new DownloadTaskDesignViewModel()
        {
            Name = "Invisible Camouflage",
            Game = "Hide and Seek Pro",
            Version = "2.5.0",
            DownloadedBytes = 330_000000,
            SizeBytes = 1000_000000,
            Status = DownloadTaskStatus.Downloading,
            Throughput = 10_000_000,
            TaskId = EntityId.From(1024),
        });

        DesignTimeDownloadTasks.AddOrUpdate(new DownloadTaskDesignViewModel()
        {
            Name = "Time Travel Mod",
            Game = "Chronos Unleashed",
            Version = "1.2.0",
            DownloadedBytes = 280_000000,
            SizeBytes = 1000_000000,
            Status = DownloadTaskStatus.Downloading,
            Throughput = 4_500_000,
            TaskId = EntityId.From(1025),
        });

        DesignTimeDownloadTasks.AddOrUpdate(new DownloadTaskDesignViewModel()
        {
            Name = "Unlimited Lives",
            Game = "Endless Quest",
            Version = "13.3.7",
            DownloadedBytes = 100_000000,
            SizeBytes = 1000_000000,
            Status = DownloadTaskStatus.Paused,
            TaskId = EntityId.From(1026),
        });

        DesignTimeDownloadTasks.AddOrUpdate(new DownloadTaskDesignViewModel()
        {
            Name = "Silent Karaoke Mode",
            Game = "Pop Star World",
            Version = "0.0.0",
            DownloadedBytes = 0,
            TaskId = EntityId.From(1027),
        });
    }

    internal void AddDownload(DownloadTaskDesignViewModel vm) => DesignTimeDownloadTasks.AddOrUpdate(vm);

    internal void ClearDownloads() => DesignTimeDownloadTasks.Clear();
}
