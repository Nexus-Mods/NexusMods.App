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
        });
    }
}
