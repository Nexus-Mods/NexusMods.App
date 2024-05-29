using Avalonia.ReactiveUI;
using ReactiveUI;

namespace NexusMods.App.UI.Overlays.Login;

public partial class LoginMessageBoxView : ReactiveUserControl<ILoginMessageBoxViewModel>
{
    public LoginMessageBoxView()
    {
        InitializeComponent();

        this.WhenActivated(d =>
            {
                this.BindCommand(ViewModel, vm => vm.OkCommand, view => view.OkButton);
                this.BindCommand(ViewModel, vm => vm.CancelCommand, view => view.CancelButton);
                this.BindCommand(ViewModel, vm => vm.CancelCommand, view => view.CloseButton);
            }
        );
    }
}

