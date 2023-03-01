using System.Reactive;
using System.Reactive.Linq;
using System.Windows.Input;
using Avalonia.Media;
using Microsoft.Extensions.Logging;
using NexusMods.App.UI.ViewModels;
using NexusMods.Networking.NexusWebApi;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Controls.TopBar;

public class TopBarViewModel : AViewModel<ITopBarViewModel>, ITopBarViewModel
{
    private readonly LoginManager _loginManager;
    private readonly ILogger<TopBarViewModel> _logger;

    public TopBarViewModel(ILogger<TopBarViewModel> logger, LoginManager loginManager)
    {
        _logger = logger;
        _loginManager = loginManager;
        LoginCommand = ReactiveCommand.CreateFromTask(Login);
    }

    private async Task Login()
    {
        _logger.LogInformation("Logging into Nexus Mods");
        await _loginManager.LoginAsync();
    }

    [Reactive]
    public bool IsLoggedIn { get; set; }
    
    [Reactive]
    public bool IsPremium { get; set; }
    
    [Reactive]
    public IImage Avatar { get; set; }

    [Reactive] public ReactiveCommand<Unit, Unit> LoginCommand { get; set; }

    [Reactive] public ReactiveCommand<Unit, Unit> LogoutCommand { get; set; } = ReactiveCommand.Create(() => { });
    
    [Reactive]
    public ReactiveCommand<Unit, Unit> MinimizeCommand { get; set; } = ReactiveCommand.Create(() => { });
    
    [Reactive]
    public ReactiveCommand<Unit, Unit> MaximizeCommand { get; set; } = ReactiveCommand.Create(() => { });

    [Reactive] public ReactiveCommand<Unit, Unit> CloseCommand { get; set; } = ReactiveCommand.Create(() => { });
}