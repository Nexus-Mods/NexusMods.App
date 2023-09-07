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

            this.WhenAnyValue(x => x.ViewModel!.HighlightedOptionViewModel!.Option)
                .SubscribeWithErrorLogging(logger: default, option =>
                {
                    HighlightedOptionDescription.Text = option?.Description ?? string.Empty;
                })
                .DisposeWith(disposables);

            this.OneWayBind(ViewModel, vm => vm.Groups, view => view.GroupItemsControl.ItemsSource)
                .DisposeWith(disposables);

            this.BindCommand(ViewModel, vm => vm.PreviousStepCommand, view => view.PreviousButton)
                .DisposeWith(disposables);

            this.BindCommand(ViewModel, vm => vm.NextStepCommand, view => view.NextButton)
                .DisposeWith(disposables);
        });
    }
}
