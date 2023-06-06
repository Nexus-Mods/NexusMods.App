using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using DynamicData;
using NexusMods.App.UI.RightContent.Downloads.ViewModels;
using ReactiveUI;

namespace NexusMods.App.UI.RightContent.Downloads;

public class InProgressDesignViewModel : AViewModel<IInProgressViewModel>, IInProgressViewModel
{
    private ReadOnlyObservableCollection<IDownloadTaskViewModel> _tasksObservable = new(new ObservableCollection<IDownloadTaskViewModel>());
    public ReadOnlyObservableCollection<IDownloadTaskViewModel> Tasks => _tasksObservable;

    private readonly CancellationTokenSource _backgroundUpdateToken = new();

    public InProgressDesignViewModel()
    {
        SourceList<IDownloadTaskViewModel> tasks = new();
        tasks.Add(new DownloadTaskDesignViewModel()
        {
            Name = "Invisible Camouflage",
            Game = "Hide and Seek Pro",
            Version = "2.5.0"
        });
        
        tasks.Add(new DownloadTaskDesignViewModel()
        {
            Name = "Time Travel Mod",
            Game = "Chronos Unleashed",
            Version = "1.2.0"
        });
        
        tasks.Add(new DownloadTaskDesignViewModel()
        {
            Name = "Unlimited Lives",
            Game = "Endless Quest",
            Version = "13.3.7"
        });

        tasks.Add(new DownloadTaskDesignViewModel()
        {
            Name = "Silent Karaoke Mode",
            Game = "Pop Star World",
            Version = "0.0.0"
        });

        this.WhenActivated(d =>
        {
            tasks.Connect()
                .Bind(out _tasksObservable)
                .Subscribe()
                .DisposeWith(d);

            _backgroundUpdateToken.DisposeWith(d);
        });
    }
}
