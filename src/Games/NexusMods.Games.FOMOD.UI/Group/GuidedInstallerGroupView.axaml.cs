using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace NexusMods.Games.FOMOD.UI;

public partial class GuidedInstallerGroupView : ReactiveUserControl<IGuidedInstallerGroupViewModel>
{
    public GuidedInstallerGroupView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            this.OneWayBind(ViewModel, vm => vm.Group.Name, view => view.GroupName.Text)
                .DisposeWith(disposables);

            this.OneWayBind(ViewModel, vm => vm.Options, view => view.OptionItemsControl.ItemsSource)
                .DisposeWith(disposables);
        });
    }
}
