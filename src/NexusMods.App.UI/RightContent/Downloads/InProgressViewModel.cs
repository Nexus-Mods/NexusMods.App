using System.Reactive.Disposables;
using DynamicData;
using NexusMods.App.UI.Overlays;
using NexusMods.App.UI.RightContent.Downloads.ViewModels;
using NexusMods.Networking.Downloaders;
using NexusMods.Networking.Downloaders.Interfaces;
using ReactiveUI;

namespace NexusMods.App.UI.RightContent.Downloads;

public class InProgressViewModel : InProgressCommonViewModel
{
    public InProgressViewModel(IDownloadService downloadService, IOverlayController overlayController)
    {
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

            SuspendCurrentTask = ReactiveCommand.Create(() => SelectedTask?.Suspend());
            SuspendAllTasks = ReactiveCommand.Create(() =>
            {
                foreach (var task in Tasks.ToArray())
                    task.Suspend();
            });

            downloadService.Downloads
                .Filter(x => x.Status != DownloadTaskStatus.Completed)
                .Transform(x => (IDownloadTaskViewModel) new DownloadTaskViewModel(x))
                .OnUI()
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
