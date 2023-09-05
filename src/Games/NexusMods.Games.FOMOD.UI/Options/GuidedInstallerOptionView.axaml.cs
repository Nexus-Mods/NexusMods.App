using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace NexusMods.Games.FOMOD.UI;

public partial class GuidedInstallerOptionView : ReactiveUserControl<IGuidedInstallerOptionViewModel>
{
    public GuidedInstallerOptionView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            this.OneWayBind(ViewModel, vm => vm.Option.Name, view => view.CheckBoxTextBlock.Text)
                .DisposeWith(disposables);

            this.Bind(ViewModel, vm => vm.IsSelected, view => view.CheckBox.IsChecked)
                .DisposeWith(disposables);
        });
    }
}

