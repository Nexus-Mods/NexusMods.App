using Avalonia.ReactiveUI;
using NexusMods.App.BuildInfo;
using NexusMods.App.UI.Controls;
using NexusMods.App.UI.Resources;
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
                            HeadingText.Text = string.Format(Language.Updater_UpdateAvailable, vm.LatestVersion);
                            TextGenericBody.Text = string.Format(Language.Updater_GenericMessage, vm.CurrentVersion, vm.LatestVersion);

                            var installationMethod = vm.InstallationMethod;

                            if (installationMethod is InstallationMethod.PackageManager)
                            {
                                TextInstructions.Text = Language.Updater_UsePackageManager;
                            }
                            else if (installationMethod is InstallationMethod.InnoSetup)
                            {
                                TextInstructions.Text = Language.Updater_UseInnoSetup;
                            }
                            else if (installationMethod is InstallationMethod.Flatpak)
                            {
                                TextInstructions.Text = Language.Updater_UseFlatpak;
                            }
                            else
                            {
                                TextInstructions.IsVisible = false;
                            }

                            // we need to change styling of the open browser button depending on if we have an asset or not
                            ButtonOpenReleaseInBrowser.Type = vm.HasAsset ? StandardButton.Types.Tertiary : StandardButton.Types.Primary;
                            ButtonOpenReleaseInBrowser.Fill = vm.HasAsset ? StandardButton.Fills.Weak : StandardButton.Fills.Strong;
                        }
                    )
                    .AddTo(disposables);

                this.BindCommand(ViewModel, vm => vm.CommandClose, view => view.ButtonClose)
                    .AddTo(disposables);

                this.BindCommand(ViewModel, vm => vm.CommandOpenReleaseInBrowser, view => view.ButtonOpenReleaseInBrowser)
                    .AddTo(disposables);

                this.BindCommand(ViewModel, vm => vm.CommandDownloadReleaseAssetInBrowser, view => view.ButtonDownloadReleaseAssetInBrowser)
                    .AddTo(disposables);

                this.OneWayBind(ViewModel, vm => vm.HasAsset, view => view.ButtonDownloadReleaseAssetInBrowser.IsVisible)
                    .AddTo(disposables);
            }
        );
    }
}
