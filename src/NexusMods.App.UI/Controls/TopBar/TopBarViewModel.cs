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
using NexusMods.Abstractions.Settings;
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
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Controls.TopBar;

[UsedImplicitly(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature)]
public class TopBarViewModel : AViewModel<ITopBarViewModel>, ITopBarViewModel
{
    private readonly ILoginManager _loginManager;
    private readonly ILogger<TopBarViewModel> _logger;

    [Reactive] public string ActiveWorkspaceTitle { get; [UsedImplicitly] set; } = string.Empty;

    public ReactiveCommand<NavigationInformation, Unit> OpenSettingsCommand { get; }

    public ReactiveCommand<NavigationInformation, Unit> ViewChangelogCommand { get; }
    public ReactiveCommand<Unit, Unit> ViewAppLogsCommand { get; }
    public ReactiveCommand<Unit, Unit> GiveFeedbackCommand { get; }

    public ReactiveCommand<Unit, Unit> LoginCommand { get; }
    public ReactiveCommand<Unit, Unit> LogoutCommand { get; }
    public ReactiveCommand<Unit, Unit> OpenNexusModsProfileCommand { get; }
    public ReactiveCommand<Unit, Unit> OpenNexusModsAccountSettingsCommand { get; }

    [Reactive] public bool IsLoggedIn { get; [UsedImplicitly] set; }
    [Reactive] public bool IsPremium { get; [UsedImplicitly] set; }

    private readonly ObservableAsPropertyHelper<IImage?> _avatar;
    public IImage? Avatar => _avatar.Value;

    [Reactive] public IAddPanelDropDownViewModel AddPanelDropDownViewModel { get; set; } = null!;

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

        if (!windowManager.TryGetActiveWindow(out var window))
        {
            throw new NotImplementedException();
        }

        var workspaceController = window.WorkspaceController;

        OpenSettingsCommand = ReactiveCommand.Create<NavigationInformation>(info =>
        {
            var page = new PageData
            {
                Context = new SettingsPageContext(),
                FactoryId = SettingsPageFactory.StaticId,
            };

            var behavior = workspaceController.GetOpenPageBehavior(page, info, Optional<PageIdBundle>.None);
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

            var behavior = workspaceController.GetOpenPageBehavior(page, info, Optional<PageIdBundle>.None);
            workspaceController.OpenPage(workspaceController.ActiveWorkspace!.Id, page, behavior);
        });

        ViewAppLogsCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            var loggingSettings = settingsManager.Get<LoggingSettings>();
            var logDirectory = loggingSettings.MainProcessLogFilePath.ToPath(fileSystem).Parent;
            await osInterop.OpenDirectory(logDirectory);
        }, // TODO: enable this once OpenDirectory has been implemented
            Observable.Return(false));

        GiveFeedbackCommand = ReactiveCommand.Create(() =>
        {
            var alphaWarningViewModel = serviceProvider.GetRequiredService<IAlphaWarningViewModel>();
            alphaWarningViewModel.WorkspaceController = workspaceController;

            overlayController.Enqueue(alphaWarningViewModel);
        });

        var canLogin = this.WhenAnyValue(x => x.IsLoggedIn).Select(isLoggedIn => !isLoggedIn);
        LoginCommand = ReactiveCommand.CreateFromTask(Login, canLogin);

        var canLogout = this.WhenAnyValue(x => x.IsLoggedIn);
        LogoutCommand = ReactiveCommand.CreateFromTask(Logout, canLogout);

        _avatar = _loginManager.AvatarObservable
            .OffUi()
            .SelectMany(LoadImage)
            .ToProperty(this, vm => vm.Avatar, scheduler: RxApp.MainThreadScheduler);

        OpenNexusModsProfileCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            var userInfo = await _loginManager.GetUserInfoAsync();
            if (userInfo is null) return;

            var userId = userInfo.UserId.Value;
            var uri = new Uri($"https://nexusmods.com/users/{userId}");
            await osInterop.OpenUrl(uri);
        });

        OpenNexusModsAccountSettingsCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            var uri = new Uri("https://users.nexusmods.com");
            await osInterop.OpenUrl(uri);
        });

        this.WhenActivated(d =>
        {
            _loginManager.IsLoggedInObservable
                .OnUI()
                .BindToVM(this, vm => vm.IsLoggedIn)
                .DisposeWith(d);

            _loginManager.IsPremiumObservable
                .OnUI()
                .BindToVM(this, vm => vm.IsPremium)
                .DisposeWith(d);

            workspaceController.WhenAnyValue(controller => controller.ActiveWorkspace!.Title)
                .Select(title => title.ToUpperInvariant())
                .BindToVM(this, vm => vm.ActiveWorkspaceTitle)
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
