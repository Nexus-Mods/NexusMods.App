using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
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

            this.OneWayBind(ViewModel, vm => vm.HighlightedOption!.Description, view => view.HighlightedOptionDescription.Text)
                .DisposeWith(disposables);

            this.OneWayBind(ViewModel, vm => vm.Groups, view => view.GroupItemsControl.ItemsSource)
                .DisposeWith(disposables);
        });
    }
}
