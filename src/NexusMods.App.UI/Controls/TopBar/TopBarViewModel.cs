using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using DynamicData.Kernel;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.NexusWebApi;
using NexusMods.App.UI.Controls.Navigation;
using NexusMods.App.UI.Overlays;
using NexusMods.App.UI.Overlays.AlphaWarning;
using NexusMods.App.UI.Pages.Settings;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Controls.TopBar;

[UsedImplicitly]
public class TopBarViewModel : AViewModel<ITopBarViewModel>, ITopBarViewModel
{
    private readonly ILoginManager _loginManager;
    private readonly ILogger<TopBarViewModel> _logger;

    public TopBarViewModel(
        IServiceProvider serviceProvider,
        ILogger<TopBarViewModel> logger,
        ILoginManager loginManager,
        IWindowManager windowManager,
        IOverlayController overlayController)
    {
        _logger = logger;
        _loginManager = loginManager;

        if (!windowManager.TryGetActiveWindow(out var window))
        {
            throw new NotImplementedException();
        }

        var workspaceController = window.WorkspaceController;

        SettingsActionCommand = ReactiveCommand.Create<NavigationInformation>(info =>
        {
            var page = new PageData
            {
                Context = new SettingsPageContext(),
                FactoryId = SettingsPageFactory.StaticId
            };

            var behavior = workspaceController.GetOpenPageBehavior(page, info, Optional<PageIdBundle>.None);
            workspaceController.OpenPage(workspaceController.ActiveWorkspace!.Id, page, behavior);
        });

        HelpActionCommand = ReactiveCommand.Create(() =>
        {
            var alphaWarningViewModel = serviceProvider.GetRequiredService<IAlphaWarningViewModel>();
            alphaWarningViewModel.WorkspaceController = workspaceController;

            overlayController.Enqueue(alphaWarningViewModel);
        });

        this.WhenActivated(d =>
        {
            var canLogin = this.WhenAnyValue(x => x.IsLoggedIn).Select(isLoggedIn => !isLoggedIn);
            LoginCommand = ReactiveCommand.CreateFromTask(Login, canLogin).DisposeWith(d);

            var canLogout = this.WhenAnyValue(x => x.IsLoggedIn);
            LogoutCommand = ReactiveCommand.CreateFromTask(Logout, canLogout).DisposeWith(d);

            _loginManager.IsLoggedInObservable
                .OnUI()
                .SubscribeWithErrorLogging(logger, x => IsLoggedIn = x)
                .DisposeWith(d);

            _loginManager.IsPremiumObservable
                .OnUI()
                .SubscribeWithErrorLogging(logger, x => IsPremium = x)
                .DisposeWith(d);

            _loginManager.AvatarObservable
                .WhereNotNull()
                .OffUi()
                .SelectMany(LoadImage)
                .WhereNotNull()
                .OnUI()
                .SubscribeWithErrorLogging(logger, x => Avatar = x)
                .DisposeWith(d);

            workspaceController.WhenAnyValue(controller => controller.ActiveWorkspace!.Title)
                .Select(title => title.ToUpperInvariant())
                .BindTo(this, vm => vm.ActiveWorkspaceTitle)
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

    [Reactive] public bool IsLoggedIn { get; set; }

    [Reactive] public bool IsPremium { get; set; }

    [Reactive] public IImage Avatar { get; set; } = Initializers.IImage;
    [Reactive] public string ActiveWorkspaceTitle { get; set; } = string.Empty;

    [Reactive] public IAddPanelDropDownViewModel AddPanelDropDownViewModel { get; set; } = null!;

    [Reactive] public ReactiveCommand<Unit, Unit> LoginCommand { get; set; } = Initializers.EnabledReactiveCommand;

    [Reactive] public ReactiveCommand<Unit, Unit> LogoutCommand { get; set; } = Initializers.DisabledReactiveCommand;

    [Reactive] public ReactiveCommand<Unit, Unit> MinimizeCommand { get; set; } = Initializers.DisabledReactiveCommand;

    [Reactive]
    public ReactiveCommand<Unit, Unit> ToggleMaximizeCommand { get; set; } = Initializers.DisabledReactiveCommand;

    [Reactive] public ReactiveCommand<Unit, Unit> CloseCommand { get; set; } = Initializers.DisabledReactiveCommand;

    public ReactiveCommand<Unit, Unit> HistoryActionCommand { get; } =
        ReactiveCommand.Create(() => { }, Observable.Return(false));

    public ReactiveCommand<Unit, Unit> UndoActionCommand { get; } =
        ReactiveCommand.Create(() => { }, Observable.Return(false));

    public ReactiveCommand<Unit, Unit> RedoActionCommand { get; } =
        ReactiveCommand.Create(() => { }, Observable.Return(false));

    public ReactiveCommand<Unit, Unit> HelpActionCommand { get; }

    public ReactiveCommand<NavigationInformation, Unit> SettingsActionCommand { get; }
}
