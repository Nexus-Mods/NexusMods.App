using System.Reactive.Disposables;
using System.Reactive.Linq;
using DynamicData;
using NexusMods.App.UI.RightContent.Downloads.ViewModels;
using NexusMods.Networking.Downloaders;
using NexusMods.Networking.Downloaders.Interfaces;
using ReactiveUI;

namespace NexusMods.App.UI.RightContent.Downloads;

public class InProgressViewModel : InProgressCommonViewModel
{
    public InProgressViewModel(DownloadService downloadService)
    {
        SourceCache<IDownloadTaskViewModel, IDownloadTask> tasks = new(_ => throw new NotImplementedException());
        
        this.WhenActivated(d =>
        {
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
}
