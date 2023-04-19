using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using NexusMods.App.UI.Extensions;
using ReactiveUI;

namespace NexusMods.App.UI.LeftMenu.Downloads;

public partial class DownloadsView : ReactiveUserControl<IDownloadsViewModel>
{
    public DownloadsView()
    {
        InitializeComponent();

        this.WhenActivated(d =>
        {
            ViewModel!.IsActive(Options.InProgress)
                .BindToActive(InProgressButton)
                .DisposeWith(d);
            ViewModel!.IsActive(Options.Completed)
                .BindToActive(CompletedButton)
                .DisposeWith(d);
            ViewModel!.IsActive(Options.History)
                .BindToActive(HistoryButton)
                .DisposeWith(d);

            InProgressButton.Command = ViewModel.CommandFor(Options.InProgress);
            CompletedButton.Command = ViewModel.CommandFor(Options.Completed);
            HistoryButton.Command = ViewModel.CommandFor(Options.History);
        });


    }
}

