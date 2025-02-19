using System.Reactive;
using System.Reactive.Disposables;
using DynamicData.Kernel;
using JetBrains.Annotations;
using NexusMods.Abstractions.Settings;
using NexusMods.App.UI.Controls.Navigation;
using NexusMods.App.UI.Pages.Changelog;
using NexusMods.App.UI.Settings;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.CrossPlatform.Process;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Overlays.AlphaWarning;

[UsedImplicitly]
public class AlphaWarningViewModel : AOverlayViewModel<IAlphaWarningViewModel>, IAlphaWarningViewModel
{
    public ReactiveCommand<Unit, Unit> ViewChangelogInAppCommand { get; }
    public ReactiveCommand<Unit, Uri> OpenDiscordCommand { get; }
    public ReactiveCommand<Unit, Uri> OpenForumsCommand { get; }
    public ReactiveCommand<Unit, Uri> OpenGitHubCommand { get; }

    public ReactiveCommand<Unit, Unit> CloseCommand { get; }

    public IWorkspaceController? WorkspaceController { get; set; }

    private readonly ISettingsManager _settingsManager;

    public AlphaWarningViewModel(
        IOSInterop osInterop,
        ISettingsManager settingsManager)
    {
        _settingsManager = settingsManager;

        OpenDiscordCommand = ReactiveCommand.Create(() => ConstantLinks.DiscordUri);
        OpenForumsCommand = ReactiveCommand.Create(() => ConstantLinks.ForumsUri);
        OpenGitHubCommand = ReactiveCommand.Create(() => ConstantLinks.GitHubUri);

        ViewChangelogInAppCommand = ReactiveCommand.Create(() =>
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

            var behavior = workspaceController.GetOpenPageBehavior(pageData, new NavigationInformation(NavigationInput.Default, Optional<OpenPageBehaviorType>.None));
            workspaceController.OpenPage(workspaceController.ActiveWorkspaceId, pageData, behavior);

            Close();
        });

        CloseCommand = ReactiveCommand.Create(() =>
        {
            _settingsManager.Update<AlphaSettings>(settings => settings with
            {
                HasShownModal = true,
            });

            Close();
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

        Controller.Enqueue(this);
        return true;
    }
}
