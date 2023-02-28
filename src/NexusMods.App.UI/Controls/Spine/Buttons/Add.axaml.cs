using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Mixins;
using Avalonia.ReactiveUI;
using NexusMods.App.UI.Controls.Spine.Buttons;
using ReactiveUI;

namespace NexusMods.App.UI.Controls.Spine.Buttons;

public partial class Add : ReactiveUserControl<AddButtonViewModel>
{
    public Add() 
    {
        InitializeComponent();
        this.WhenActivated(disposables =>
        {
            ViewModel.WhenAnyValue(vm => vm.IsActive)
                .StartWith(false)
                .Subscribe(SetClasses)
                .DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.Click, v => v.Button)
                .DisposeWith(disposables);
        });
    }
    
    private void SetClasses(bool isActive)
    {
        if (isActive)
        {
            Button.Classes.Add("Active");
            Button.Classes.Remove("Inactive");
        }
        else
        {
            Button.Classes.Remove("Active");
            Button.Classes.Add("Inactive");
        }
    }
}