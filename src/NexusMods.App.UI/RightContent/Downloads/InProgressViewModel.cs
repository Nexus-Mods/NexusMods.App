using System.Reactive.Disposables;
using System.Reactive.Linq;
using DynamicData;
using NexusMods.App.UI.Overlays;
using NexusMods.App.UI.Overlays.Download.Cancel;
using NexusMods.App.UI.RightContent.Downloads.ViewModels;
using NexusMods.App.UI.Windows;
using NexusMods.Networking.Downloaders;
using NexusMods.Networking.Downloaders.Interfaces;
using ReactiveUI;

namespace NexusMods.App.UI.RightContent.Downloads;

public class InProgressViewModel : InProgressCommonViewModel
{
    public InProgressViewModel(DownloadService downloadService, IOverlayController overlayController)
    {
        SourceCache<IDownloadTaskViewModel, IDownloadTask> tasks = new(_ => throw new NotImplementedException());

        this.WhenActivated(d =>
        {
            ShowCancelDialog = ReactiveCommand.Create(async () =>
            {
                if (SelectedTask == null)
                    return;
                
                var result = await overlayController.ShowCancelDownloadOverlay(SelectedTask);
                if (result) 
                    CancelSelectedTask();
            });

            // Subscribe to started tasks
            downloadService.StartedTasks
                .Subscribe(task =>
                {
                    tasks.Edit(x =>
                    {
                        x.AddOrUpdate(new DownloadTaskViewModel(task), task);
                    });
                });

            // Subscribe to completed tasks and remove them from tasks list
            downloadService.CompletedTasks
                .Merge(downloadService.CancelledTasks) // Cancelled and completed are treated the same here.
                .Subscribe(task =>
                {
                    tasks.Remove(task);
                });

            tasks.Connect()
                .Bind(out TasksObservable)
                .Subscribe()
                .DisposeWith(d);

            // This is necessary due to inheritance,
            // WhenActivated gets fired in wrong order and
            // parent classes need to be able to properly subscribe
            // here.
            this.RaisePropertyChanged(nameof(Tasks));
        });
    }

    protected override void UpdateWindowInfo()
    {
        // Poll Tasks
        foreach (var task in Tasks)
        {
            if (task is DownloadTaskViewModel vm)
                vm.Poll();
        }

        // Update Base
        base.UpdateWindowInfo();
    }
}
