using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.UI;
using NexusMods.App.UI.Controls;
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

    public DownloadsLeftMenuViewModel(
        WorkspaceId workspaceId,
        IWorkspaceController workspaceController,
        IServiceProvider serviceProvider)
    {
        WorkspaceId = workspaceId;

        var gameRegistry = serviceProvider.GetRequiredService<IGameRegistry>();

        // All Downloads menu item
        LeftMenuItemAllDownloads = new LeftMenuItemViewModel(
            workspaceController,
            WorkspaceId,
            new PageData
            {
                FactoryId = DownloadsPageFactory.StaticId,
                Context = new DownloadsPageContext(),
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
                Context = new DownloadsPageContext(), // TODO: Add completed filter context when implemented
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
                .Transform(gameInstallation => CreatePerGameDownloadItem(gameInstallation, workspaceController, workspaceId))
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
        WorkspaceId workspaceId)
    {
        // TODO: Replace with proper game-specific context when per-game filtering is implemented
        return new LeftMenuItemViewModel(
            workspaceController,
            workspaceId,
            new PageData
            {
                FactoryId = DownloadsPageFactory.StaticId,
                Context = new DownloadsPageContext(), // TODO: Add game filter context
            }
        )
        {
            Text = new StringComponent(string.Format(Language.DownloadsLeftMenu_GameSpecificDownloads, gameInstallation.Game.Name)),
            Icon = IconValues.FolderEditOutline,
        };
    }
}