using System.Reactive;
using System.Reactive.Disposables;
using DynamicData.Kernel;
using NexusMods.Abstractions.Settings;
using NexusMods.App.UI.Controls.Navigation;
using NexusMods.App.UI.Pages.Changelog;
using NexusMods.App.UI.Settings;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.CrossPlatform.Process;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Overlays.AlphaWarning;

public class AlphaWarningViewModel : AViewModel<IAlphaWarningViewModel>, IAlphaWarningViewModel
{
    [Reactive] public bool IsActive { get; set; }

    // NOTE(erri120): from https://github.com/Nexus-Mods/NexusMods.App/issues/1376
    private static readonly Uri DiscordUri = new("https://discord.gg/y7NfQWyRkj");
    private static readonly Uri ForumsUri = new("https://forums.nexusmods.com/forum/9052-nexus-mods-app/");
    private static readonly Uri GitHubUri = new("https://github.com/Nexus-Mods/NexusMods.App");

    public ReactiveCommand<Unit, Unit> ViewChangelogCommand { get; }
    public ReactiveCommand<Unit, Uri> OpenDiscordCommand { get; }
    public ReactiveCommand<Unit, Uri> OpenForumsCommand { get; }
    public ReactiveCommand<Unit, Uri> OpenGitHubCommand { get; }

    public ReactiveCommand<Unit, Unit> CloseCommand { get; }

    public IWorkspaceController? WorkspaceController { get; set; }

    private readonly ISettingsManager _settingsManager;
    private readonly IOverlayController _overlayController;

    public AlphaWarningViewModel(
        IOSInterop osInterop,
        ISettingsManager settingsManager,
        IOverlayController overlayController)
    {
        _settingsManager = settingsManager;
        _overlayController = overlayController;

        OpenDiscordCommand = ReactiveCommand.Create(() => DiscordUri);
        OpenForumsCommand = ReactiveCommand.Create(() => ForumsUri);
        OpenGitHubCommand = ReactiveCommand.Create(() => GitHubUri);

        ViewChangelogCommand = ReactiveCommand.Create(() =>
        {
            var workspaceController = WorkspaceController;
            if (workspaceController is null) return;

            var pageData = new PageData
            {
                Context = new ChangelogPageContext
                {
                    TargetVersion = null,
                },
                FactoryId = ChangelogPageFactory.StaticId,
            };

            var behavior = workspaceController.GetOpenPageBehavior(pageData, new NavigationInformation(NavigationInput.Default, Optional<OpenPageBehaviorType>.None), Optional<PageIdBundle>.None);
            workspaceController.OpenPage(workspaceController.ActiveWorkspace!.Id, pageData, behavior);
        });

        CloseCommand = ReactiveCommand.Create(() =>
        {
            _settingsManager.Update<AlphaSettings>(settings => settings with
            {
                HasShownModal = true,
            });

            IsActive = false;
        });

        this.WhenActivated(disposables =>
        {
            this.WhenAnyObservable(
                vm => vm.OpenDiscordCommand,
                vm => vm.OpenForumsCommand,
                vm => vm.OpenGitHubCommand)
                .SubscribeWithErrorLogging(uri =>
                {
                    _ = Task.Run(async () =>
                    {
                        await osInterop.OpenUrl(uri);
                    });
                })
                .DisposeWith(disposables);
        });
    }

    public bool MaybeShow()
    {
        if (_settingsManager.Get<AlphaSettings>().HasShownModal) return false;

        _overlayController.SetOverlayContent(new SetOverlayItem(this));
        return true;
    }
}
