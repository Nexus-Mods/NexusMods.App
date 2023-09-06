using System.Reactive.Disposables;
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

            this.OneWayBind(ViewModel, vm => vm.IsEnabled, view => view.CheckBox.IsEnabled)
                .DisposeWith(disposables);

            this.WhenAnyValue(x => x.ViewModel!.Option)
                .WhereNotNull()
                .SubscribeWithErrorLogging(logger: default, option =>
                {
                    ToolTip.SetTip(CheckBoxTextBlock, option.HoverText);
                })
                .DisposeWith(disposables);

            this.Bind(ViewModel, vm => vm.IsSelected, view => view.CheckBox.IsChecked)
                .DisposeWith(disposables);
        });
    }
}

