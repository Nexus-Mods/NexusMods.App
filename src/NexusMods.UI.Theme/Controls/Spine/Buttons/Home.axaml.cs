using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Mixins;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace NexusMods.UI.Theme.Controls.Spine.Buttons;

public partial class Home : ReactiveUserControl<SpineButtonViewModel>
{
    public Home()
    {
        InitializeComponent();
        this.WhenActivated(disposables =>
        {
            this.OneWayBind(ViewModel, vm => vm.IsActive, v => v.Toggle.IsChecked)
                .DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.Click, v => v.Toggle.Command)
                .DisposeWith(disposables);
            this.WhenAnyValue(vm => vm.ViewModel.Click)
                .Subscribe(f => { })
                .DisposeWith(disposables);
        });
    }
}