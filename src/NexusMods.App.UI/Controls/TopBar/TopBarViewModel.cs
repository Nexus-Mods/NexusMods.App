using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Microsoft.Extensions.Logging;
using NexusMods.Networking.NexusWebApi.NMA;
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
        LoginCommand = ReactiveCommand.CreateFromTask(Login, _loginManager.IsLoggedIn.OnUI().Select(b => !b));
        LogoutCommand = ReactiveCommand.CreateFromTask(Logout, _loginManager.IsLoggedIn.OnUI().Select(b => b));

        this.WhenActivated(d =>
        {
            _loginManager.IsLoggedIn
                .SubscribeWithErrorLogging(logger, x => IsLoggedIn = x)
                .DisposeWith(d);

            _loginManager.IsPremium
                .SubscribeWithErrorLogging(logger, x => IsPremium = x)
                .DisposeWith(d);

            _loginManager.Avatar
                .WhereNotNull()
                .SelectMany(LoadImage)
                .WhereNotNull()
                .SubscribeWithErrorLogging(logger, x => Avatar = x)
                .DisposeWith(d);
        });
    }

    private async Task<IImage?> LoadImage(Uri? uri)
    {
        if (uri == null) return null;
        try
        {
            var client = new HttpClient();
            var stream = await client.GetByteArrayAsync(uri);
            return new Bitmap(new MemoryStream(stream));
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to load image: {Uri}", uri);
            return null;
        }
    }

    private async Task Login()
    {
        _logger.LogInformation("Logging into Nexus Mods");
        await _loginManager.LoginAsync();
    }

    private async Task Logout()
    {
        _logger.LogInformation("Logging out of Nexus Mods");
        await _loginManager.Logout();
    }

    [Reactive] public bool ShowWindowControls { get; set; } = false;

    [Reactive]
    public bool IsLoggedIn { get; set; }

    [Reactive]
    public bool IsPremium { get; set; }

    [Reactive] public IImage Avatar { get; set; } = Initializers.IImage;

    [Reactive] public ReactiveCommand<Unit, Unit> LoginCommand { get; set; }

    [Reactive] public ReactiveCommand<Unit, Unit> LogoutCommand { get; set; }

    [Reactive]
    public ReactiveCommand<Unit, Unit> MinimizeCommand { get; set; } = ReactiveCommand.Create(() => { });

    [Reactive]
    public ReactiveCommand<Unit, Unit> ToggleMaximizeCommand { get; set; } = ReactiveCommand.Create(() => { });

    [Reactive] public ReactiveCommand<Unit, Unit> CloseCommand { get; set; } = ReactiveCommand.Create(() => { });
}
