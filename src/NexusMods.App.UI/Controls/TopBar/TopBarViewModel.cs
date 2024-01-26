using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.NexusWebApi;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Controls.TopBar;

public class TopBarViewModel : AViewModel<ITopBarViewModel>, ITopBarViewModel
{
    private readonly ILoginManager _loginManager;
    private readonly ILogger<TopBarViewModel> _logger;

    public TopBarViewModel(ILogger<TopBarViewModel> logger, ILoginManager loginManager)
    {
        _logger = logger;
        _loginManager = loginManager;

        this.WhenActivated(d =>
        {
            var canLogin = this.WhenAnyValue(x => x.IsLoggedIn).Select(isLoggedIn => !isLoggedIn);
            LoginCommand = ReactiveCommand.CreateFromTask(Login, canLogin).DisposeWith(d);

            var canLogout = this.WhenAnyValue(x => x.IsLoggedIn);
            LogoutCommand = ReactiveCommand.CreateFromTask(Logout, canLogout).DisposeWith(d);

            _loginManager.IsLoggedIn
                .OnUI()
                .SubscribeWithErrorLogging(logger, x => IsLoggedIn = x)
                .DisposeWith(d);

            _loginManager.IsPremium
                .OnUI()
                .SubscribeWithErrorLogging(logger, x => IsPremium = x)
                .DisposeWith(d);

            _loginManager.Avatar
                .WhereNotNull()
                .OffUi()
                .SelectMany(LoadImage)
                .WhereNotNull()
                .OnUI()
                .SubscribeWithErrorLogging(logger, x => Avatar = x)
                .DisposeWith(d);
        });
    }

    private async Task<IImage?> LoadImage(Uri uri)
    {
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

    [Reactive] public ReactiveCommand<Unit, Unit> LoginCommand { get; set; } = Initializers.EnabledReactiveCommand;

    [Reactive] public ReactiveCommand<Unit, Unit> LogoutCommand { get; set; } = Initializers.DisabledReactiveCommand;

    [Reactive] public ReactiveCommand<Unit, Unit> MinimizeCommand { get; set; } = Initializers.DisabledReactiveCommand;

    [Reactive]
    public ReactiveCommand<Unit, Unit> ToggleMaximizeCommand { get; set; } = Initializers.DisabledReactiveCommand;

    [Reactive] public ReactiveCommand<Unit, Unit> CloseCommand { get; set; } = Initializers.DisabledReactiveCommand;
}
