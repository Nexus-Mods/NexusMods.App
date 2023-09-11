using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using NexusMods.App.UI;
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
                .SubscribeWithErrorLogging(logger: default, _ =>
                {
                    HighlightedOptionImage.IsVisible = false;
                });

            this.OneWayBind(ViewModel, vm => vm.Groups, view => view.GroupItemsControl.ItemsSource)
                .DisposeWith(disposables);

            this.BindCommand(ViewModel, vm => vm.PreviousStepCommand, view => view.PreviousButton)
                .DisposeWith(disposables);

            this.BindCommand(ViewModel, vm => vm.NextStepCommand, view => view.NextButton)
                .DisposeWith(disposables);
        });
    }
}
