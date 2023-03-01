using System.Windows.Input;
using Avalonia.Media;
using Microsoft.Extensions.Logging;
using NexusMods.App.UI.ViewModels;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Controls.TopBar;

public class TopBarViewModel : AViewModel, ITopBarViewModel
{
    public TopBarViewModel(ILogger<TopBarViewModel> logger)
    {
    }

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
    
    [Reactive]
    public ICommand MinimizeCommand { get; set; }
    
    [Reactive]
    public ICommand MaximizeCommand { get; set; }
    
    [Reactive]
    public ICommand CloseCommand { get; set; }
}