using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.UI;
using NexusMods.App.UI.Controls;
using NexusMods.App.UI.Helpers;
using NexusMods.App.UI.LeftMenu.Items;
using NexusMods.App.UI.Pages.Downloads;
using NexusMods.App.UI.Resources;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.UI.Sdk.Icons;
using ReactiveUI;

namespace NexusMods.App.UI.LeftMenu.Downloads;

[UsedImplicitly]
public class DownloadsLeftMenuViewModel : AViewModel<IDownloadsLeftMenuViewModel>, IDownloadsLeftMenuViewModel
{
    public WorkspaceId WorkspaceId { get; }
    public ILeftMenuItemViewModel LeftMenuItemAllDownloads { get; }
    public ILeftMenuItemViewModel LeftMenuItemAllCompleted { get; }

    private ReadOnlyObservableCollection<ILeftMenuItemViewModel> _leftMenuItemsPerGameDownloads = new([]);
    public ReadOnlyObservableCollection<ILeftMenuItemViewModel> LeftMenuItemsPerGameDownloads => _leftMenuItemsPerGameDownloads;

    private readonly ILogger<DownloadsLeftMenuViewModel> _logger;

    public DownloadsLeftMenuViewModel(
        WorkspaceId workspaceId,
        IWorkspaceController workspaceController,
        IServiceProvider serviceProvider)
    {
        WorkspaceId = workspaceId;
        _logger = serviceProvider.GetRequiredService<ILogger<DownloadsLeftMenuViewModel>>();

        var gameRegistry = serviceProvider.GetRequiredService<IGameRegistry>();

        // All Downloads menu item
        LeftMenuItemAllDownloads = new LeftMenuItemViewModel(
            workspaceController,
            WorkspaceId,
            new PageData
            {
                FactoryId = DownloadsPageFactory.StaticId,
                Context = new AllDownloadsPageContext(),
            }
        )
        {
            Text = new StringComponent(Language.DownloadsLeftMenu_AllDownloads),
            Icon = IconValues.Download,
        };

        // All Completed menu item
        LeftMenuItemAllCompleted = new LeftMenuItemViewModel(
            workspaceController,
            WorkspaceId,
            new PageData
            {
                FactoryId = DownloadsPageFactory.StaticId,
                Context = new CompletedDownloadsPageContext(), // TODO: Add completed filter context when implemented
            }
        )
        {
            Text = new StringComponent(Language.DownloadsLeftMenu_AllCompleted),
            Icon = IconValues.CheckCircle,
        };

        // Per-game downloads (dynamic)
        this.WhenActivated(disposable =>
        {
            gameRegistry.InstalledGames
                .ToObservableChangeSet()
                .Transform(gameInstallation => CreatePerGameDownloadItem(gameInstallation, workspaceController, workspaceId, _logger))
                .DisposeMany()
                .OnUI()
                .Bind(out _leftMenuItemsPerGameDownloads)
                .Subscribe()
                .DisposeWith(disposable);
        });
    }

    private static ILeftMenuItemViewModel CreatePerGameDownloadItem(
        GameInstallation gameInstallation,
        IWorkspaceController workspaceController,
        WorkspaceId workspaceId,
        ILogger<DownloadsLeftMenuViewModel> logger)
    {
        // TODO: Replace with proper game-specific context when per-game filtering is implemented
        var viewModel = new LeftMenuItemViewModel(
            workspaceController,
            workspaceId,
            new PageData
            {
                FactoryId = DownloadsPageFactory.StaticId,
                Context = new GameSpecificDownloadsPageContext(gameInstallation.Game.GameId), // TODO: Add game filter context
            }
        )
        {
            Text = new StringComponent(string.Format(Language.DownloadsLeftMenu_GameSpecificDownloads, gameInstallation.Game.Name)),
            Icon = IconValues.FolderEditOutline, // Initial fallback icon
        };

        // Load game icon asynchronously
        Observable.FromAsync(() => ImageHelper.LoadGameIconAsync((IGame)gameInstallation.Game, logger))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(bitmap => viewModel.Icon = ImageHelper.CreateIconValueFromBitmap(bitmap, IconValues.FolderEditOutline));

        return viewModel;
    }
}