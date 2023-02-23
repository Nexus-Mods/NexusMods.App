using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Mixins;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace NexusMods.UI.Theme.Controls.Spine.Buttons;

public partial class Add : ReactiveUserControl<SpineButtonViewModel>
{

    public Add() 
    {
        InitializeComponent();
        this.WhenActivated(disposables =>
        {
            this.OneWayBind(ViewModel, vm => vm.IsActive, v => v.Toggle.IsChecked)
                .DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.Click, v => v.Toggle.Command)
                .DisposeWith(disposables);
        });
    }


    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}