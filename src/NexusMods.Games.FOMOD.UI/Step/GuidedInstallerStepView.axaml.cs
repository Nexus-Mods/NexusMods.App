using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using NexusMods.App.UI;
using NexusMods.Games.FOMOD.UI.Resources;
using ReactiveUI;

namespace NexusMods.Games.FOMOD.UI;

public partial class GuidedInstallerStepView : ReactiveUserControl<IGuidedInstallerStepViewModel>
{
    public GuidedInstallerStepView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            this.OneWayBind(ViewModel, vm => vm.InstallationStep!.Name, view => view.StepName.Text)
                .DisposeWith(disposables);

            this.OneWayBind(ViewModel, vm => vm.Groups, view => view.GroupItemsControl.ItemsSource)
                .DisposeWith(disposables);

            this.WhenAnyValue(view => view.ViewModel!.HighlightedOptionImage)
                .SubscribeWithErrorLogging(image =>
                {
                    var tmp = HighlightedOptionImage.Source;
                    HighlightedOptionImage.IsVisible = image is not null;
                    HighlightedOptionImage.Source = image;

                    if (tmp is IDisposable disposable) disposable.Dispose();
                })
                .DisposeWith(disposables);

            this.WhenAnyValue(view => view.ViewModel!.HighlightedOptionViewModel)
                .SubscribeWithErrorLogging(optionVM =>
                {
                    HighlightedOptionDescription.Text = optionVM?.Option.Description ?? Language.GuidedInstallerStepView_GuidedInstallerStepView_No_additional_details_available;

                    PreviewTitleTextBox.Text = optionVM?.Option.Name;
                    PreviewHeaderDescriptionIcon.IsVisible = optionVM?.Option.Description is not null;
                    PreviewHeaderImageIcon.IsVisible = optionVM?.Option.Image is not null;
                    HighlightedOptionImage.IsVisible = optionVM?.Option.Image is not null;
                })
                .DisposeWith(disposables);

            this.WhenAnyValue(view => view.ViewModel!.ShowInstallationCompleteScreen)
                .SubscribeWithErrorLogging(showInstallationCompleteScreen =>
                {
                    if (showInstallationCompleteScreen)
                    {
                        GroupsGrid.IsVisible = false;
                        InstallationCompleteScreenTextBlock.IsVisible = true;
                        StepName.Text = Language.GuidedInstallerStepView_GuidedInstallerStepView_Installation_complete;
                    }
                    else
                    {
                        InstallationCompleteScreenTextBlock.IsVisible = false;
                        GroupsGrid.IsVisible = true;
                        StepName.Text = ViewModel?.InstallationStep?.Name;
                    }
                })
                .DisposeWith(disposables);

            this.OneWayBind(ViewModel, vm => vm.FooterStepperViewModel, view => view.FooterStepperViewHost.ViewModel)
                .DisposeWith(disposables);
        });
    }
}
