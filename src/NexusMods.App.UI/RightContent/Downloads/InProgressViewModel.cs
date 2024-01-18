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
            downloadService.Downloads
                .Filter(x => x.Status != DownloadTaskStatus.Completed)
                .Transform(x => (IDownloadTaskViewModel)new DownloadTaskViewModel(x))
                .OnUI()
                .Bind(out TasksObservable)
                .Subscribe()
                .DisposeWith(d);

            ShowCancelDialogCommand = ReactiveCommand.Create(async () =>
            {
                if (SelectedTasks.Items.Any())
                {
                    var result = await overlayController.ShowCancelDownloadOverlay(SelectedTasks.Items);
                    if (result)
                        CancelTasks(SelectedTasks.Items);
                }
            }, Tasks.ToObservableChangeSet()
                .AutoRefresh(task => task.Status)
                .Select(_ => Tasks.Any()));

            SuspendSelectedTasksCommand = ReactiveCommand.Create(
                () => { SuspendTasks(SelectedTasks.Items); },
                SelectedTasks.Connect()
                    .AutoRefresh(task => task.Status)
                    .Select(_ => SelectedTasks.Items.Any(task => task.Status == DownloadTaskStatus.Downloading)))
                .DisposeWith(d);

            ResumeSelectedTasksCommand = ReactiveCommand.Create(
                () => { ResumeTasks(SelectedTasks.Items); },
                SelectedTasks.Connect()
                    .AutoRefresh(task => task.Status)
                    .Select(_ => SelectedTasks.Items.Any(task => task.Status == DownloadTaskStatus.Paused)))
                .DisposeWith(d);

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


            Tasks.ToObservableChangeSet()
                .AutoRefresh(task => task.Status)
                .Subscribe(_ =>
                {
                    UpdateWindowInfo();
                    ActiveDownloadCount = Tasks.Count(task => task.Status == DownloadTaskStatus.Downloading);
                    HasDownloads = Tasks.Any();
                });

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
