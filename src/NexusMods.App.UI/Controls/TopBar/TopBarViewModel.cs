using System.Reactive;
using System.Windows.Input;
using Avalonia.Media;
using Microsoft.Extensions.Logging;
using NexusMods.App.UI.ViewModels;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Controls.TopBar;

public class TopBarViewModel : AViewModel<ITopBarViewModel>, ITopBarViewModel
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
    public ReactiveCommand<Unit, Unit> MinimizeCommand { get; set; } = ReactiveCommand.Create(() => { });
    
    [Reactive]
    public ReactiveCommand<Unit, Unit> MaximizeCommand { get; set; } = ReactiveCommand.Create(() => { });

    [Reactive] public ReactiveCommand<Unit, Unit> CloseCommand { get; set; } = ReactiveCommand.Create(() => { });
}