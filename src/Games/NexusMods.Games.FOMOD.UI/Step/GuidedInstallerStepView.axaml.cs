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
            this.OneWayBind(ViewModel, vm => vm.ModName, view => view.ModName.Text)
                .DisposeWith(disposables);

            this.OneWayBind(ViewModel, vm => vm.InstallationStep!.Name, view => view.StepName.Text)
                .DisposeWith(disposables);

            this.OneWayBind(ViewModel, vm => vm.HighlightedOptionDescription, view => view.HighlightedOptionDescription.Text)
                .DisposeWith(disposables);

            ViewModel?.HighlightedOptionImageObservable
                .SubscribeWithErrorLogging(logger: default, image =>
                {
                    HighlightedOptionImage.IsVisible = true;
                    HighlightedOptionImage.Source = image;
                })
                .DisposeWith(disposables);

            this.WhenAnyValue(x => x.ViewModel!.HighlightedOptionViewModel)
                .SubscribeWithErrorLogging(logger: default, optionVM =>
                {
                    HighlightedOptionImage.IsVisible = false;
                    PreviewTitleTextBox.Text = optionVM?.Option.Name;
                    PreviewHeaderDescriptionIcon.IsVisible = optionVM?.Option.Description is not null;
                    PreviewHeaderImageIcon.IsVisible = optionVM?.Option.ImageUrl is not null;
                });

            this.OneWayBind(ViewModel, vm => vm.Groups, view => view.GroupItemsControl.ItemsSource)
                .DisposeWith(disposables);

            this.OneWayBind(ViewModel, vm => vm.FooterStepperViewModel, view => view.FooterStepperViewHost.ViewModel)
                .DisposeWith(disposables);

            this.WhenAnyValue(x => x.ViewModel!.ShowInstallationCompleteScreen)
                .SubscribeWithErrorLogging(logger: default, showInstallationCompleteScreen =>
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
                });
        });
    }
}
