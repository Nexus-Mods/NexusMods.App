using System.ComponentModel;
using System.Reactive.Disposables;
using Avalonia.Controls;
using DynamicData;
using NexusMods.App.UI.RightContent.Downloads.ViewModels;
using NexusMods.App.UI.RightContent.LoadoutGrid;
using NexusMods.App.UI.RightContent.LoadoutGrid.Columns;
using NexusMods.App.UI.RightContent.LoadoutGrid.Columns.DownloadGameName;
using NexusMods.App.UI.RightContent.LoadoutGrid.Columns.DownloadName;
using NexusMods.App.UI.RightContent.LoadoutGrid.Columns.DownloadSize;
using NexusMods.App.UI.RightContent.LoadoutGrid.Columns.DownloadStatus;
using NexusMods.App.UI.RightContent.LoadoutGrid.Columns.DownloadVersion;
using NexusMods.Networking.Downloaders.Interfaces;
using ReactiveUI;
using DownloadNameView = NexusMods.App.UI.RightContent.LoadoutGrid.Columns.DownloadName.DownloadNameView;

namespace NexusMods.App.UI.RightContent.Downloads;

public class InProgressDesignViewModel : InProgressCommonViewModel
{
    public InProgressDesignViewModel()
    {
        SourceList<IDownloadTaskViewModel> tasks = new();
        tasks.Add(new DownloadTaskDesignViewModel()
        {
            Name = "Invisible Camouflage",
            Game = "Hide and Seek Pro",
            Version = "2.5.0",
            DownloadedBytes = 330_000000,
            SizeBytes = 1000_000000,
            Status = DownloadTaskStatus.Downloading,
            Throughput = 10_000_000
        });
        
        tasks.Add(new DownloadTaskDesignViewModel()
        {
            Name = "Time Travel Mod",
            Game = "Chronos Unleashed",
            Version = "1.2.0",
            DownloadedBytes = 280_000000,
            SizeBytes = 1000_000000,
            Status = DownloadTaskStatus.Downloading,
            Throughput = 4_500_000
        });
        
        tasks.Add(new DownloadTaskDesignViewModel()
        {
            Name = "Unlimited Lives",
            Game = "Endless Quest",
            Version = "13.3.7",
            DownloadedBytes = 100_000000,
            SizeBytes = 1000_000000,
            Status = DownloadTaskStatus.Paused
        });

        tasks.Add(new DownloadTaskDesignViewModel()
        {
            Name = "Silent Karaoke Mode",
            Game = "Pop Star World",
            Version = "0.0.0",
            DownloadedBytes = 0
        });

        // Make Columns
        var columns = new SourceCache<IDataGridColumnFactory, ColumnType>(x => x.Type);
        columns.Edit(x =>
        {
            x.AddOrUpdate(
                new DataGridColumnDesignFactory<IDownloadNameViewModel, IDownloadTaskViewModel>(
                    x => new DownloadNameView()
                    {
                        ViewModel = new DownloadNameViewModel() { Row = x }
                    }, ColumnType.DownloadName)
                {
                    Width = new DataGridLength(1, DataGridLengthUnitType.Star)
                });
            
            x.AddOrUpdate(
                new DataGridColumnDesignFactory<IDownloadVersionViewModel, IDownloadTaskViewModel>(
                    x => new DownloadVersionView()
                    {
                        ViewModel = new DownloadVersionViewModel() { Row = x }
                    }, ColumnType.DownloadVersion));
            
            x.AddOrUpdate(
                new DataGridColumnDesignFactory<IDownloadGameNameViewModel, IDownloadTaskViewModel>(
                    x => new DownloadGameNameView()
                    {
                        ViewModel = new DownloadGameNameViewModel() { Row = x }
                    }, ColumnType.DownloadGameName));
            
            x.AddOrUpdate(
                new DataGridColumnDesignFactory<IDownloadSizeViewModel, IDownloadTaskViewModel>(
                    x => new DownloadSizeView()
                    {
                        ViewModel = new DownloadSizeViewModel() { Row = x }
                    }, ColumnType.DownloadSize));
            
            x.AddOrUpdate(
                new DataGridColumnDesignFactory<IDownloadStatusViewModel, IDownloadTaskViewModel>(
                    x => new DownloadStatusView()
                    {
                        ViewModel = new DownloadStatusViewModel() { Row = x }
                    }, ColumnType.DownloadStatus));
        });
        
        this.WhenActivated(d =>
        {
            tasks.Connect()
                .Bind(out TasksObservable)
                .Subscribe()
                .DisposeWith(d);
            
            columns.Connect()
                .Bind(out FilteredColumns)
                .Subscribe()
                .DisposeWith(d);
            
            // This is necessary due to inheritance,
            // WhenActivated gets fired in wrong order and
            // parent classes need to be able to properly subscribe
            // here.
            this.RaisePropertyChanged(nameof(Tasks));
            this.RaisePropertyChanged(nameof(Columns));
        });
    }
}
