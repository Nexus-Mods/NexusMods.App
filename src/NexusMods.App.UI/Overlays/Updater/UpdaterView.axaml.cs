using Avalonia.ReactiveUI;
using NexusMods.App.BuildInfo;
using ReactiveUI;
using R3;

namespace NexusMods.App.UI.Overlays.Updater;

public partial class UpdaterView : ReactiveUserControl<IUpdaterViewModel>
{
    public UpdaterView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            this.WhenAnyValue(view => view.ViewModel)
                .WhereNotNull()
                .SubscribeWithErrorLogging(vm =>
                {
                    TextHeader.Text = $"Update available {vm.LatestVersion}";
                    TextGenericBody.Text = $"Your current version {vm.CurrentVersion} can be updated to the latest version {vm.LatestVersion}. Be sure to close the app completely before updating.";

                    var installationMethod = vm.InstallationMethod;
                    if (installationMethod is InstallationMethod.PackageManager)
                    {
                        TextInstructions.Text = "You can update the app using the package manager you used to install the app with.";
                    } else if (installationMethod is InstallationMethod.InnoSetup)
                    {
                        TextInstructions.Text = "You can update the app by clicking the downloading setup and running it after closing the app.";
                    } else if (installationMethod is InstallationMethod.Flatpak)
                    {
                        TextInstructions.Text = "You can update the app using Flatpak.";
                    }
                    else
                    {
                        TextInstructions.IsVisible = false;
                    }
                })
                .AddTo(disposables);

            this.BindCommand(ViewModel, vm => vm.CommandClose, view => view.ButtonClose)
                .AddTo(disposables);

            this.BindCommand(ViewModel, vm => vm.CommandOpenReleaseInBrowser, view => view.ButtonOpenReleaseInBrowser)
                .AddTo(disposables);

            this.BindCommand(ViewModel, vm => vm.CommandDownloadReleaseAssetInBrowser, view => view.ButtonDownloadReleaseAssetInBrowser)
                .AddTo(disposables);

            this.OneWayBind(ViewModel, vm => vm.HasAsset, view => view.ButtonDownloadReleaseAssetInBrowser.IsVisible)
                .AddTo(disposables);
        });
    }
}

