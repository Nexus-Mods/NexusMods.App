using Avalonia.ReactiveUI;
using R3;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.CollectionDownload;

public partial class ManualDownloadRequiredOverlayView : ReactiveUserControl<IManualDownloadRequiredOverlayViewModel>
{
    public ManualDownloadRequiredOverlayView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            this.BindCommand(ViewModel, vm => vm.CommandCancel, view => view.ButtonCancel)
                .AddTo(disposables);

            this.BindCommand(ViewModel, vm => vm.CommandOpenBrowser, view => view.ButtonOpenBrowser)
                .AddTo(disposables);

            this.BindCommand(ViewModel, vm => vm.CommandAddFile, view => view.ButtonAddFile)
                .AddTo(disposables);

            this.BindCommand(ViewModel, vm => vm.CommandTryAgain, view => view.ButtonTryAgain)
                .AddTo(disposables);

            this.OneWayBind(ViewModel, vm => vm.DownloadName, view => view.DownloadName.Text)
                .AddTo(disposables);

            this.OneWayBind(ViewModel, vm => vm.ExpectedHash, view => view.ExpectedHash.Text)
                .AddTo(disposables);

            this.OneWayBind(ViewModel, vm => vm.ExpectedSize, view => view.ExpectedSize.Text)
                .AddTo(disposables);

            this.OneWayBind(ViewModel, vm => vm.Instructions, view => view.Instructions.Text)
                .AddTo(disposables);

            this.OneWayBind(ViewModel, vm => vm.HasInstructions, view => view.Instructions.IsVisible)
                .AddTo(disposables);

            this.OneWayBind(ViewModel, vm => vm.ReceivedHash, view => view.ReceivedHash.Text)
                .AddTo(disposables);

            this.WhenAnyValue(
                view => view.ViewModel!.IsCheckingFile,
                view => view.ViewModel!.IsIncorrectFile)
                .SubscribeWithErrorLogging(tuple =>
                {
                    var (isCheckingFile, isIncorrectFile) = tuple;

                    var showInfo = !isCheckingFile && !isIncorrectFile;
                    var showChecking = isCheckingFile && !isIncorrectFile;
                    var showError = !isCheckingFile && isIncorrectFile;

                    InfoPanel.IsVisible = showInfo;
                    CheckingPanel.IsVisible = showChecking;
                    ErrorPanel.IsVisible = showError;

                    ButtonOpenBrowser.IsVisible = showInfo;
                    ButtonAddFile.IsVisible = showInfo;

                    ButtonTryAgain.IsVisible = showError;
                }).AddTo(disposables);
        });
    }
}

