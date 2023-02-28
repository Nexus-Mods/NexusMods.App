using System.Reactive.Linq;
using Avalonia.Controls.Mixins;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace NexusMods.App.UI.Controls.Spine.Buttons;

public partial class Home : ReactiveUserControl<HomeButtonViewModel>
{
    public Home()
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
            this.WhenAnyValue(vm => vm.ViewModel.Click)
                .Subscribe(f => { })
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