using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Media.Imaging;
using DynamicData;
using DynamicData.Binding;
using DynamicData.Kernel;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.UI;
using NexusMods.App.UI.Controls.LoadoutBadge;
using NexusMods.App.UI.Controls.Navigation;
using NexusMods.App.UI.Controls.Spine.Buttons;
using NexusMods.App.UI.Controls.Spine.Buttons.Download;
using NexusMods.App.UI.Controls.Spine.Buttons.Icon;
using NexusMods.App.UI.Controls.Spine.Buttons.Image;
using NexusMods.App.UI.LeftMenu;
using NexusMods.App.UI.Pages.LoadoutPage;
using NexusMods.App.UI.Pages.MyGames;
using NexusMods.App.UI.Resources;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceAttachments;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.MnemonicDB.Abstractions;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Controls.Spine;

[UsedImplicitly]
public class SpineViewModel : AViewModel<ISpineViewModel>, ISpineViewModel
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SpineViewModel> _logger;
    private readonly IWindowManager _windowManager;
    private readonly ISynchronizerService _syncService;

    private ReadOnlyObservableCollection<IImageButtonViewModel> _loadoutSpineItems = new([]);
    private static readonly LoadoutSpineEntriesComparer LoadoutComparerInstance = new();
    public ReadOnlyObservableCollection<IImageButtonViewModel> LoadoutSpineItems => _loadoutSpineItems;
    public IIconButtonViewModel Home { get; }
    public IIconButtonViewModel AddLoadout { get; }
    public ISpineDownloadButtonViewModel Downloads { get; }
    private IList<ISpineItemViewModel> _specialSpineItems = new List<ISpineItemViewModel>();

    private ISpineItemViewModel? _activeSpineItem;

    private Dictionary<WorkspaceId, ILeftMenuViewModel> _leftMenus = new([]);
    private readonly IConnection _conn;
    [Reactive] public ILeftMenuViewModel? LeftMenuViewModel { get; private set; }

    public SpineViewModel(
        IServiceProvider serviceProvider,
        ILogger<SpineViewModel> logger,
        IConnection conn,
        IWindowManager windowManager,
        IIconButtonViewModel addButtonViewModel,
        IIconButtonViewModel homeButtonViewModel,
        ISpineDownloadButtonViewModel spineDownloadsButtonViewModel,
        IWorkspaceAttachmentsFactoryManager workspaceAttachmentsFactory)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _windowManager = windowManager;
        _conn = conn;
        _syncService = serviceProvider.GetRequiredService<ISynchronizerService>();

        // Setup the special spine items
        Home = homeButtonViewModel;
        Home.Name = Language.SpineHomeButton_ToolTip_Home;
        Home.WorkspaceContext = new HomeContext();
        _specialSpineItems.Add(Home);
        Home.Click = ReactiveCommand.Create(NavigateToHome);

        AddLoadout = addButtonViewModel;
        AddLoadout.Name = "Add";
        AddLoadout.WorkspaceContext = new HomeContext();
        _specialSpineItems.Add(AddLoadout);
        AddLoadout.Click = ReactiveCommand.Create(NavigateToMyGames);

        Downloads = spineDownloadsButtonViewModel;
        Downloads.WorkspaceContext = new DownloadsContext();
        _specialSpineItems.Add(Downloads);

        var workspaceController = windowManager.ActiveWorkspaceController;

        this.WhenActivated(disposables =>
            {
                var loadouts = Loadout.ObserveAll(_conn);

                loadouts
                    .Filter(loadout => loadout.IsVisible())
                    .TransformAsync(async loadout =>
                        {
                            await using var iconStream = await ((IGame)loadout.InstallationInstance.Game).Icon.GetStreamAsync();

                            var vm = serviceProvider.GetRequiredService<IImageButtonViewModel>();
                            vm.Name = loadout.InstallationInstance.Game.Name + " - " + loadout.Name;
                            vm.Image = LoadImageFromStream(iconStream);
                            vm.LoadoutBadgeViewModel = new LoadoutBadgeViewModel(_conn, _syncService, hideOnSingleLoadout: true);
                            vm.LoadoutBadgeViewModel.LoadoutValue = loadout;
                            vm.WorkspaceContext = new LoadoutContext { LoadoutId = loadout.LoadoutId };
                            vm.Click = ReactiveCommand.Create(() => ChangeToLoadoutWorkspace(loadout.LoadoutId));
                            vm.IsActive = false;

                            if (workspaceController.ActiveWorkspace.Context is LoadoutContext activeLoadoutContext &&
                                activeLoadoutContext.LoadoutId == loadout.LoadoutId)
                            {
                                SetActiveItem(vm);
                            }
                            
                            return vm;
                        }
                    )
                    .OnUI()
                    .OnItemRemoved(loadoutSpineItem =>
                        {
                            if (loadoutSpineItem.WorkspaceContext is LoadoutContext loadoutContext)
                                workspaceController.UnregisterWorkspaceByContext<LoadoutContext>(context => loadoutContext == context);
                        })
                .SortAndBind(out _loadoutSpineItems, LoadoutComparerInstance)
                .SubscribeWithErrorLogging()
                .DisposeWith(disposables);

            // Create Left Menus for each workspace on demand
            workspaceController.AllWorkspaces
                .ToObservableChangeSet()
                .OnItemAdded(workspace =>
                {
                    if (_leftMenus.TryGetValue(workspace.Id, out _))
                    {
                        return;
                    }
                        
                    try
                    {
                        var leftMenu = workspaceAttachmentsFactory.CreateLeftMenuFor(
                            workspace.Context,
                            workspace.Id,
                            workspaceController
                        );

                        if (leftMenu == null)
                        {
                            throw new InvalidDataException("LeftMenu factory returned a null view model");
                        }
                        
                        _leftMenus.Add(workspace.Id, leftMenu);
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, "Exception while creating left menu for context {Context}", workspace.Context);
                    }
                })
                .OnItemRemoved(workspace => _leftMenus.Remove(workspace.Id, out _))
                .SubscribeWithErrorLogging()
                .DisposeWith(disposables);

            // Navigate away from the Loadout workspace if the Loadout is removed
            loadouts
                .OnUI()
                .OnItemRemoved(loadout =>
                    {
                        if (workspaceController.ActiveWorkspace.Context is LoadoutContext activeLoadoutContext &&
                            activeLoadoutContext.LoadoutId == loadout.LoadoutId)
                        {
                            workspaceController.ChangeOrCreateWorkspaceByContext<HomeContext>(() => new PageData
                                {
                                    FactoryId = MyGamesPageFactory.StaticId,
                                    Context = new MyGamesPageContext(),
                                }
                            );
                        }
                    }, false
                )
                .SubscribeWithErrorLogging()
                .DisposeWith(disposables);

            // Update the LeftMenuViewModel when the active workspace changes
            workspaceController.WhenAnyValue(controller => controller.ActiveWorkspace)
                .Select(workspace => workspace.Id)
                .Select(workspaceId => _leftMenus.GetValueOrDefault(workspaceId))
                .BindToVM(this, vm => vm.LeftMenuViewModel)
                .DisposeWith(disposables);

            // Update the active spine item when the active workspace changes
            workspaceController
                .WhenAnyValue(controller => controller.ActiveWorkspace)
                .Select(workspace => workspace.Context)
                .WhereNotNull()
                .SubscribeWithErrorLogging(context =>
                    {
                        var itemToActivate = _specialSpineItems
                            .Concat(_loadoutSpineItems)
                            .FirstOrDefault(spineItem => spineItem.WorkspaceContext?.Equals(context) == true);

                        SetActiveItem(itemToActivate);
                    }
                )
                .DisposeWith(disposables);
            
            }
        );
    }

    private void SetActiveItem(ISpineItemViewModel? itemToActivate)
    {
        if (itemToActivate == null)
            return;

        if (_activeSpineItem != null)
            _activeSpineItem.IsActive = false;

        itemToActivate.IsActive = true;
        _activeSpineItem = itemToActivate;
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

    public void NavigateToHome()
    {
        var workspaceController = _windowManager.ActiveWorkspaceController;

        workspaceController.ChangeOrCreateWorkspaceByContext<HomeContext>(() => new PageData
            {
                FactoryId = MyGamesPageFactory.StaticId,
                Context = new MyGamesPageContext()
            }
        );
    }

    private void ChangeToLoadoutWorkspace(LoadoutId loadoutId)
    {
        var workspaceController = _windowManager.ActiveWorkspaceController;

        workspaceController.ChangeOrCreateWorkspaceByContext(
            context => context.LoadoutId == loadoutId,
            () => new PageData
            {
                FactoryId = LoadoutPageFactory.StaticId,
                Context = new LoadoutPageContext
                {
                    LoadoutId = loadoutId,
                    GroupScope = Optional<LoadoutItemGroupId>.None,
                }
            },
            () => new LoadoutContext
            {
                LoadoutId = loadoutId
            }
        );
    }

    private void NavigateToMyGames()
    {
        var workspaceController = _windowManager.ActiveWorkspaceController;

        var pageData = new PageData
        {
            FactoryId = MyGamesPageFactory.StaticId,
            Context = new MyGamesPageContext(),
        };

        var ws = workspaceController.ChangeOrCreateWorkspaceByContext<HomeContext>(() => pageData);
        var behavior = workspaceController.GetOpenPageBehavior(pageData, NavigationInformation.From(NavigationInput.Default));
        workspaceController.OpenPage(ws.Id, pageData, behavior);
    }

    private class LoadoutSpineEntriesComparer : IComparer<IImageButtonViewModel>
    {
        public int Compare(IImageButtonViewModel? x, IImageButtonViewModel? y)
        {
            var xloadout = x?.LoadoutBadgeViewModel?.LoadoutValue;
            var yloadout = y?.LoadoutBadgeViewModel?.LoadoutValue;

            if (xloadout == null) return yloadout == null ? 0 : -1;
            if (yloadout == null) return 1;

            if (xloadout.Value.Value.Installation.Path != yloadout.Value.Value.Installation.Path)
                return DateTimeOffset.Compare(xloadout.Value.Value.Installation.GetCreatedAt(), yloadout.Value.Value.Installation.GetCreatedAt());

            return DateTimeOffset.Compare(xloadout.Value.Value.GetCreatedAt(), yloadout.Value.Value.GetCreatedAt());
        }
    }
}
