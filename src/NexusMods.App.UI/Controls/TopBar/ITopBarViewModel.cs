using System.Windows.Input;
using Avalonia.Media;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Controls.TopBar;

public interface ITopBarViewModel
{
    public bool IsLoggedIn { get; }
    public bool IsPremium { get; }
    public IImage Avatar { get; }
    
    public ICommand LoginCommand { get; }
    public ICommand LogoutCommand { get; }
}