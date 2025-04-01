using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.NexusWebApi;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.Abstractions.Settings;
using NexusMods.Abstractions.Telemetry;
using NexusMods.Abstractions.UI;
using NexusMods.App.UI.Controls.Navigation;
using NexusMods.App.UI.Overlays;
using NexusMods.App.UI.Overlays.AlphaWarning;
using NexusMods.App.UI.Pages.Changelog;
using NexusMods.App.UI.Pages.Settings;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.CrossPlatform;
using NexusMods.CrossPlatform.Process;
using NexusMods.Paths;
using NexusMods.Telemetry;
using R3;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using ReactiveCommand = ReactiveUI.ReactiveCommand;
using Unit = System.Reactive.Unit;

namespace NexusMods.App.UI.Controls.TopBar;

[UsedImplicitly(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature)]
public class TopBarViewModel : AViewModel<ITopBarViewModel>, ITopBarViewModel
{
    private readonly ILoginManager _loginManager;
    private readonly ILogger<TopBarViewModel> _logger;
    
    [Reactive] public string ActiveWorkspaceTitle { get; [UsedImplicitly] set; } = string.Empty;
    [Reactive] public string ActiveWorkspaceSubtitle { get; [UsedImplicitly] set; } = string.Empty;

    public ReactiveUI.ReactiveCommand<NavigationInformation, Unit> OpenSettingsCommand { get; }

    public ReactiveUI.ReactiveCommand<NavigationInformation, Unit> ViewChangelogCommand { get; }
    public ReactiveUI.ReactiveCommand<Unit, Unit> ViewAppLogsCommand { get; }
    public ReactiveUI.ReactiveCommand<Unit, Unit> ShowWelcomeMessageCommand { get; }    
    public ReactiveUI.ReactiveCommand<Unit, Unit> OpenDiscordCommand { get; }
    public ReactiveUI.ReactiveCommand<Unit, Unit> OpenForumsCommand { get; }
    public ReactiveUI.ReactiveCommand<Unit, Unit> OpenGitHubCommand { get; }
    public ReactiveUI.ReactiveCommand<Unit, Unit> OpenStatusPageCommand { get; }
    public R3.ReactiveCommand<R3.Unit, R3.Unit> LoginCommand { get; }
    public ReactiveUI.ReactiveCommand<Unit, Unit> LogoutCommand { get; }
    public ReactiveUI.ReactiveCommand<Unit, Unit> OpenNexusModsProfileCommand { get; }
    public ReactiveUI.ReactiveCommand<Unit, Unit> OpenNexusModsPremiumCommand { get; }
    public ReactiveUI.ReactiveCommand<Unit, Unit> OpenNexusModsAccountSettingsCommand { get; }

    [Reactive] public bool IsLoggedIn { get; [UsedImplicitly] set; }
    [Reactive] public UserRole UserRole { get; [UsedImplicitly] set; }

    [Reactive] public IImage? Avatar { get; private set; }
    
    [Reactive] public string? Username { get; set; } = string.Empty;

    [Reactive] public IAddPanelDropDownViewModel AddPanelDropDownViewModel { get; set; } = null!;

    [Reactive] public IPanelTabViewModel? SelectedTab { get; set; }

    public TopBarViewModel(
        IServiceProvider serviceProvider,
        ILogger<TopBarViewModel> logger,
        ILoginManager loginManager,
        IWindowManager windowManager,
        IOverlayController overlayController,
        IOSInterop osInterop,
        ISettingsManager settingsManager,
        IFileSystem fileSystem)
    {
        _logger = logger;
        _loginManager = loginManager;

        var workspaceController = windowManager.ActiveWorkspaceController;

        OpenSettingsCommand = ReactiveCommand.Create<NavigationInformation>(info =>
        {
            var page = new PageData
            {
                Context = new SettingsPageContext(),
                FactoryId = SettingsPageFactory.StaticId,
            };

            var behavior = workspaceController.GetOpenPageBehavior(page, info);
            var workspace = workspaceController.ChangeOrCreateWorkspaceByContext<HomeContext>(() => page);
            workspaceController.OpenPage(workspace.Id, page, behavior);
        });

        ViewChangelogCommand = ReactiveCommand.Create<NavigationInformation>(info =>
        {
            var page = new PageData
            {
                Context = new ChangelogPageContext
                {
                    TargetVersion = null,
                },
                FactoryId = ChangelogPageFactory.StaticId,
            };

            var behavior = workspaceController.GetOpenPageBehavior(page, info);
            workspaceController.OpenPage(workspaceController.ActiveWorkspace.Id, page, behavior);

            Tracking.AddEvent(Events.Help.ViewChangelog, metadata: new EventMetadata(name: null));
        });

        ViewAppLogsCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            var loggingSettings = settingsManager.Get<LoggingSettings>();
            var logPath = loggingSettings.MainProcessLogFilePath.ToPath(fileSystem);
            await osInterop.OpenFileInDirectory(logPath);

            Tracking.AddEvent(Events.Help.ViewAppLogs, metadata: new EventMetadata(name: null));
        });

        ShowWelcomeMessageCommand = ReactiveCommand.Create(() =>
        {
            var alphaWarningViewModel = serviceProvider.GetRequiredService<IAlphaWarningViewModel>();
            alphaWarningViewModel.WorkspaceController = workspaceController;

            overlayController.Enqueue(alphaWarningViewModel);
            Tracking.AddEvent(Events.Help.GiveFeedback, metadata: new EventMetadata(name: null));
        });

        var canLogin = this.WhenAnyValue(x => x.IsLoggedIn).Select(isLoggedIn => !isLoggedIn).ToObservable();
        LoginCommand = canLogin.ToReactiveCommand<R3.Unit, R3.Unit>(async (_, _) =>
        {
            await Login();
            return R3.Unit.Default;
        }, awaitOperation: AwaitOperation.Parallel, configureAwait: false);

        var canLogout = this.WhenAnyValue(x => x.IsLoggedIn);
        LogoutCommand = ReactiveCommand.CreateFromTask(Logout, canLogout);

        OpenNexusModsProfileCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            var userInfo = await _loginManager.GetUserInfoAsync();
            if (userInfo is null) return;

            var uri = NexusModsUrlBuilder.GetProfileUri(userInfo.UserId);
            await osInterop.OpenUrl(uri);
        });

        OpenNexusModsAccountSettingsCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            await osInterop.OpenUrl(NexusModsUrlBuilder.UserSettingsUri);
        });

        OpenNexusModsPremiumCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            await osInterop.OpenUrl(NexusModsUrlBuilder.UpgradeToPremiumUri);
        });
        
        OpenDiscordCommand = ReactiveCommand.CreateFromTask(async () => { await osInterop.OpenUrl(ConstantLinks.DiscordUri); });

        OpenForumsCommand = ReactiveCommand.CreateFromTask(async () => { await osInterop.OpenUrl(ConstantLinks.ForumsUri); });

        OpenGitHubCommand = ReactiveCommand.CreateFromTask(async () => { await osInterop.OpenUrl(ConstantLinks.GitHubUri); });
        
        OpenStatusPageCommand = ReactiveCommand.CreateFromTask(async () => { await osInterop.OpenUrl(ConstantLinks.StatusPageUri); });

        this.WhenActivated(d =>
        {
            _loginManager.AvatarObservable
                .SelectMany(LoadImage)
                .OnUI()
                .SubscribeWithErrorLogging(image => Avatar = image)
                .DisposeWith(d);

            _loginManager.IsLoggedInObservable
                .OnUI()
                .BindToVM(this, vm => vm.IsLoggedIn)
                .DisposeWith(d);

            _loginManager.UserRoleObservable
                .OnUI()
                .BindToVM(this, vm => vm.UserRole)
                .DisposeWith(d);
            
            _loginManager.UserInfoObservable
                .Select(u => u?.Name)
                .ObserveOnUIThreadDispatcher()
                .Subscribe(name => Username = name ?? "");
            
            workspaceController.WhenAnyValue(controller => controller.ActiveWorkspace.Title)
                .BindToVM(this, vm => vm.ActiveWorkspaceTitle)
                .DisposeWith(d);
            
            workspaceController.WhenAnyValue(controller => controller.ActiveWorkspace.Subtitle)
                .BindToVM(this, vm => vm.ActiveWorkspaceSubtitle)
                .DisposeWith(d);

            workspaceController.WhenAnyValue(controller => controller.ActiveWorkspace.SelectedTab)
                .BindToVM(this, vm => vm.SelectedTab)
                .DisposeWith(d);
        });
    }

    private async Task<IImage?> LoadImage(Uri? uri)
    {
        if (uri is null) return null;

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
}
