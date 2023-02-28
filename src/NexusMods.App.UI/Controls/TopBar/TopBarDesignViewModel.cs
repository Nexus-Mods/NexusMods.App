using System.Reactive.Linq;
using System.Windows.Input;
using Avalonia.Media;
using NexusMods.App.UI.ViewModels;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Controls.TopBar;

public class TopBarDesignViewModel : AViewModel, ITopBarViewModel
{
    [Reactive]
    public bool IsLoggedIn { get; set; }
    
    [Reactive]
    public bool IsPremium { get; set; }
    
    [Reactive]
    public IImage Avatar { get; set; }
    
    [Reactive]
    public ICommand LoginCommand { get; set; }
    
    [Reactive]
    public ICommand LogoutCommand { get; set; }

    public TopBarDesignViewModel()
    {
        IsLoggedIn = false;
        IsPremium = true;
        
        LogoutCommand = ReactiveCommand.Create(ToggleLogin, this.WhenAnyValue(vm => vm.IsLoggedIn));
        LoginCommand = ReactiveCommand.Create(ToggleLogin, this.WhenAnyValue(vm => vm.IsLoggedIn)
            .Select(x => !x));
    }

    private void ToggleLogin()
    {
        IsLoggedIn = !IsLoggedIn;
    }
}