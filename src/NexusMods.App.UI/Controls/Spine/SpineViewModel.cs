using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Media.Imaging;
using DynamicData;
using DynamicData.Binding;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Loadouts;
using NexusMods.App.UI.Controls.Spine.Buttons.Download;
using NexusMods.App.UI.Controls.Spine.Buttons.Icon;
using NexusMods.App.UI.Controls.Spine.Buttons.Image;
using NexusMods.App.UI.LeftMenu;
using NexusMods.App.UI.LeftMenu.Loadout;
using NexusMods.App.UI.Pages.Downloads;
using NexusMods.App.UI.Pages.LoadoutGrid;
using NexusMods.App.UI.Pages.MyGames;
using NexusMods.App.UI.Resources;
using NexusMods.App.UI.Resources;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceAttachments;
using NexusMods.App.UI.WorkspaceSystem;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Controls.Spine;

[UsedImplicitly]
public class SpineViewModel : AViewModel<ISpineViewModel>, ISpineViewModel
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SpineViewModel> _logger;
    private readonly IWindowManager _windowManager;

    [Reactive] public ILeftMenuViewModel? LeftMenuViewModel { get; private set; }

    private ReadOnlyObservableCollection<ILeftMenuViewModel> _leftMenus =
        Initializers.ReadOnlyObservableCollection<ILeftMenuViewModel>();



    public IIconButtonViewModel Home { get; }

    public ISpineDownloadButtonViewModel Downloads { get; }

    private ReadOnlyObservableCollection<IImageButtonViewModel> _loadouts =
        Initializers.ReadOnlyObservableCollection<IImageButtonViewModel>();

    public ReadOnlyObservableCollection<IImageButtonViewModel> Loadouts => _loadouts;

    public SpineViewModel(
        IServiceProvider serviceProvider,
        ILogger<SpineViewModel> logger,
        ILoadoutRegistry loadoutRegistry,
        IWindowManager windowManager,
        IIconButtonViewModel addButtonViewModel,
        IIconButtonViewModel homeButtonViewModel,
        ISpineDownloadButtonViewModel spineDownloadsButtonViewModel,
        IWorkspaceAttachmentsFactoryManager leftMenuFactory)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _windowManager = windowManager;

        Home = homeButtonViewModel;
        Home.Name = Language.SpineHomeButton_ToolTip_Home;
        Downloads = spineDownloadsButtonViewModel;

        if (!_windowManager.TryGetActiveWindow(out var currentWindow)) return;
        var workspaceController = currentWindow.WorkspaceController;

        this.WhenActivated(disposables =>
        {
            loadoutRegistry.Loadouts
                .TransformAsync(async loadout =>
                {
                    await using var iconStream = await ((IGame)loadout.Installation.Game).Icon.GetStreamAsync();

                    var vm = serviceProvider.GetRequiredService<IImageButtonViewModel>();
                    vm.Name = loadout.Name;
                    vm.Image = LoadImageFromStream(iconStream);
                    vm.IsActive = false;
                    vm.Click = ReactiveCommand.Create(() => ChangeToLoadoutWorkspace(loadout.LoadoutId));
                    vm.Tag = loadout.LoadoutId;
                    return vm;
                })
                .OnUI()
                .Bind(out _loadouts)
                .SubscribeWithErrorLogging()
                .DisposeWith(disposables);

            // Create Left Menus for each workspace on demand
            workspaceController.AllWorkspaces
                .ToObservableChangeSet()
                .Transform( workspace =>
                {
                    var leftMenu = leftMenuFactory.CreateLeftMenu(workspace.Context, workspace.Id, workspaceController);
                    // This should never be null, since there should be a factory for each context type, but in case
                    return leftMenu ?? new EmptyLeftMenuViewModel(workspace.Id);
                })
                .Bind(out _leftMenus)
                .SubscribeWithErrorLogging()
                .DisposeWith(disposables);

            // Update the LeftMenuViewModel when the active workspace changes
            workspaceController.
                WhenAnyValue(controller => controller.ActiveWorkspace)
                .Select(workspace => workspace?.Id)
                .Select(workspaceId => _leftMenus.FirstOrDefault(menu => menu.WorkspaceId == workspaceId))
                .BindToVM(this, vm => vm.LeftMenuViewModel)
                .DisposeWith(disposables);

            Home.Click = ReactiveCommand.Create(NavigateToHome);
            Downloads.Click = ReactiveCommand.Create(NavigateToDownloads);

            workspaceController
                .WhenAnyValue(controller => controller.ActiveWorkspace)
                .Select(workspace => workspace?.Context)
                .SubscribeWithErrorLogging(context =>
                {
                    switch (context)
                    {
                        case LoadoutContext loadoutContext:
                            ClearActiveSpineButton();
                            ActivateLoadoutSpineButton(loadoutContext.LoadoutId);
                            break;
                        case HomeContext:
                            ClearActiveSpineButton();
                            Home.IsActive = true;
                            break;
                        case DownloadsContext:
                            ClearActiveSpineButton();
                            Downloads.IsActive = true;
                            break;
                        default:
                            ClearActiveSpineButton();
                            break;
                    }
                }).DisposeWith(disposables);

            // For now just show the Home workspace on startup
            // TODO: remove this in favour of restoring the last active workspace when workspace saving is implemented
            NavigateToHome();
        });
    }

    private Bitmap LoadImageFromStream(Stream iconStream)
    {
        try
        {
            return Bitmap.DecodeToWidth(iconStream, 48);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Skia image load error, while loading image from stream");
            // Null images are fine, they will be ignored
            return null!;
        }
    }

    private void NavigateToHome()
    {
        if (!_windowManager.TryGetActiveWindow(out var window)) return;
        var workspaceController = window.WorkspaceController;

        workspaceController.ChangeOrCreateWorkspaceByContext<HomeContext>(() => new PageData
        {
            FactoryId = MyGamesPageFactory.StaticId,
            Context = new MyGamesPageContext()
        });
    }

    private void ChangeToLoadoutWorkspace(LoadoutId loadoutId)
    {
        if (!_windowManager.TryGetActiveWindow(out var window)) return;
        var workspaceController = window.WorkspaceController;

        workspaceController.ChangeOrCreateWorkspaceByContext(
            context => context.LoadoutId == loadoutId,
            () => new PageData
            {
                FactoryId = LoadoutGridPageFactory.StaticId,
                Context = new LoadoutGridContext
                {
                    LoadoutId = loadoutId
                }
            },
            () => new LoadoutContext(loadoutId)
        );
    }

    private void NavigateToDownloads()
    {
        if (!_windowManager.TryGetActiveWindow(out var window)) return;
        var workspaceController = window.WorkspaceController;

        workspaceController.ChangeOrCreateWorkspaceByContext<DownloadsContext>(() => new PageData
        {
            FactoryId = InProgressPageFactory.StaticId,
            Context = new InProgressPageContext()
        });
    }

    private void ClearActiveSpineButton()
    {
        Home.IsActive = false;
        Downloads.IsActive = false;
        foreach (var loadout in Loadouts)
        {
            loadout.IsActive = false;
        }
    }

    private void ActivateLoadoutSpineButton(LoadoutId loadoutId)
    {
        var loadoutButton = Loadouts.FirstOrDefault(button =>
        {
            if (button.Tag is LoadoutId id)
            {
                return id == loadoutId;
            }

            return false;
        });
        if (loadoutButton != null)
        {
            loadoutButton.IsActive = true;
        }
    }
}
