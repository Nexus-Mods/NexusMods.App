using System.Reactive.Disposables;
using Avalonia.Controls;
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
            this.OneWayBind(ViewModel, vm => vm.ModName, view => view.ModName.Text)
                .DisposeWith(disposables);

            this.OneWayBind(ViewModel, vm => vm.InstallationStep!.Name, view => view.StepName.Text)
                .DisposeWith(disposables);

            this.OneWayBind(ViewModel, vm => vm.Groups, view => view.GroupItemsControl.ItemsSource)
                .DisposeWith(disposables);

            ViewModel?.HighlightedOptionImageObservable
                .SubscribeWithErrorLogging(image =>
                {
                    HighlightedOptionImage.IsVisible = true;
                    HighlightedOptionImage.Source = image;
                })
                .DisposeWith(disposables);

            this.WhenAnyValue(x => x.ViewModel!.HighlightedOptionViewModel)
                .SubscribeWithErrorLogging(optionVM =>
                {
                    HighlightedOptionImage.IsVisible = false;
                    HighlightedOptionDescription.Text = optionVM?.Option.Description ?? Language.GuidedInstallerStepView_GuidedInstallerStepView_No_additional_details_available;

                    PreviewTitleTextBox.Text = optionVM?.Option.Name;
                    PreviewHeaderDescriptionIcon.IsVisible = optionVM?.Option.Description is not null;
                    PreviewHeaderImageIcon.IsVisible = optionVM?.Option.ImageUrl is not null;
                })
                .DisposeWith(disposables);

            this.WhenAnyValue(x => x.ViewModel!.ShowInstallationCompleteScreen)
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
