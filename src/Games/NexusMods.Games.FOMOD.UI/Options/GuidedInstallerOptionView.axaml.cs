using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.ReactiveUI;
using NexusMods.App.UI;
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

            this.WhenAnyValue(x => x.ViewModel!.Option)
                .Where(option => !string.IsNullOrWhiteSpace(option.HoverText))
                .SubscribeWithErrorLogging(logger: default, option => ToolTip.SetTip(CheckBoxTextBlock, option.HoverText))
                .DisposeWith(disposables);

            this.Bind(ViewModel, vm => vm.IsSelected, view => view.CheckBox.IsChecked)
                .DisposeWith(disposables);
        });
    }
}

