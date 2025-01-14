using System.Reactive.Disposables;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace NexusMods.App.UI.Overlays;

public partial class UpgradeToPremiumView : ReactiveUserControl<IUpgradeToPremiumViewModel>
{
    public UpgradeToPremiumView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            this.BindCommand(ViewModel, vm => vm.CommandCancel, view => view.ButtonCancel)
                .DisposeWith(disposables);

            this.BindCommand(ViewModel, vm => vm.CommandUpgrade, view => view.ButtonUpgrade)
                .DisposeWith(disposables);

            this.OneWayBind(ViewModel, vm => vm.MarkdownRendererViewModel, view => view.ViewModelViewHostMarkdownRenderer.ViewModel)
                .DisposeWith(disposables);
        });
    }
}

