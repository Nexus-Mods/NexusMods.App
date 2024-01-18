using System.Reactive.Disposables;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using NexusMods.App.UI.Overlays;
using NexusMods.App.UI.RightContent.Downloads.ViewModels;
using NexusMods.Networking.Downloaders.Interfaces;
using ReactiveUI;

namespace NexusMods.App.UI.RightContent.Downloads;

public class InProgressViewModel : InProgressCommonViewModel
{
    public InProgressViewModel(IDownloadService downloadService, IOverlayController overlayController)
    {
        this.WhenActivated(d =>
        {
            ShowCancelDialogCommand = ReactiveCommand.Create(async () =>
            {
                // if (SelectedTask == null)
                //     return;

                // var result = await overlayController.ShowCancelDownloadOverlay(SelectedTask);
                // if (result)
                //     SelectedTask.Cancel();
            });

            SuspendSelectedTasksCommand = ReactiveCommand.Create(
                () => { SuspendTasks(SelectedTasks.Items); },
                SelectedTasks.Connect()
                    .AutoRefresh(task => task.Status)
                    .Select(_ => SelectedTasks.Items.Any(task => task.Status == DownloadTaskStatus.Downloading)));

            ResumeSelectedTasksCommand = ReactiveCommand.Create(
                () => { ResumeTasks(SelectedTasks.Items); },
                SelectedTasks.Connect()
                    .AutoRefresh(task => task.Status)
                    .Select(_ => SelectedTasks.Items.Any(task => task.Status == DownloadTaskStatus.Paused)));

            SuspendAllTasksCommand = ReactiveCommand.Create(
                () => { SuspendTasks(Tasks); },
                 Tasks.ToObservableChangeSet()
                    .AutoRefresh(task => task.Status)
                    .Select(_ => Tasks.Any(task => task.Status == DownloadTaskStatus.Downloading)));

            ResumeAllTasksCommand = ReactiveCommand.Create(
                () => { ResumeTasks(Tasks); },
                Tasks.ToObservableChangeSet()
                    .AutoRefresh(task => task.Status)
                    .Select(_ => Tasks.Any(task => task.Status == DownloadTaskStatus.Paused)));

            downloadService.Downloads
                .Filter(x => x.Status != DownloadTaskStatus.Completed)
                .Transform(x => (IDownloadTaskViewModel)new DownloadTaskViewModel(x))
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
