using Avalonia.Controls.Mixins;
using Avalonia.ReactiveUI;
using NexusMods.App.UI.ViewModels;
using ReactiveUI;

namespace NexusMods.App.UI.Views;

public partial class MainWindow : ReactiveWindow<MainWindowViewModel>
{
    public MainWindow()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            this.OneWayBind(ViewModel, vm => vm.Spine, v => v.Spine.ViewModel)
                .DisposeWith(disposables);
            
            this.OneWayBind(ViewModel, vm => vm.RightContent, v => v.RightContent.ViewModel)
                .DisposeWith(disposables);

            ViewModel.WhenAnyValue(v => v.RightContent)
                .Subscribe(_ => { })
                .DisposeWith(disposables);
        });
    }
}