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
using NexusMods.App.UI.Controls.Spine.Buttons;
using NexusMods.App.UI.Controls.Spine.Buttons.Download;
using NexusMods.App.UI.Controls.Spine.Buttons.Icon;
using NexusMods.App.UI.Controls.Spine.Buttons.Image;
using NexusMods.App.UI.LeftMenu;
using NexusMods.App.UI.Pages.Downloads;
using NexusMods.App.UI.Pages.LoadoutGrid;
using NexusMods.App.UI.Pages.MyGames;
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

    private ReadOnlyObservableCollection<IImageButtonViewModel> _loadoutSpineItems = new([]);
    public ReadOnlyObservableCollection<IImageButtonViewModel> LoadoutSpineItems => _loadoutSpineItems;
    public IIconButtonViewModel Home { get; }
    public ISpineDownloadButtonViewModel Downloads { get; }
    private IList<ISpineItemViewModel> _specialSpineItems = new List<ISpineItemViewModel>();

    private ISpineItemViewModel? _activeSpineItem;

    private ReadOnlyObservableCollection<ILeftMenuViewModel> _leftMenus = new([]);
    [Reactive] public ILeftMenuViewModel? LeftMenuViewModel { get; private set; }

    public SpineViewModel(
        IServiceProvider serviceProvider,
        ILogger<SpineViewModel> logger,
        ILoadoutRegistry loadoutRegistry,
        IWindowManager windowManager,
        IIconButtonViewModel addButtonViewModel,
        IIconButtonViewModel homeButtonViewModel,
        ISpineDownloadButtonViewModel spineDownloadsButtonViewModel,
        IWorkspaceAttachmentsFactoryManager workspaceAttachmentsFactory)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _windowManager = windowManager;

        // Setup the special spine items
        Home = homeButtonViewModel;
        Home.Name = Language.SpineHomeButton_ToolTip_Home;
        Home.WorkspaceContext = new HomeContext();
        _specialSpineItems.Add(Home);
        Home.Click = ReactiveCommand.Create(NavigateToHome);

        Downloads = spineDownloadsButtonViewModel;
        Downloads.WorkspaceContext = new DownloadsContext();
        _specialSpineItems.Add(Downloads);
        Downloads.Click = ReactiveCommand.Create(NavigateToDownloads);
        
        if (!_windowManager.TryGetActiveWindow(out var currentWindow)) return;
        var workspaceController = currentWindow.WorkspaceController;

        this.WhenActivated(disposables =>
            {
                loadoutRegistry.LoadoutRootChanges
                    .Transform(loadoutId => (loadoutId, loadout: loadoutRegistry.Get(loadoutId)))
                    .Filter(tuple => tuple.loadout is { IsMarkerLoadout: false })
                    .TransformAsync(async tuple =>
                        {
                            var loadoutId = tuple.loadoutId;
                            var loadout = tuple.loadout!;

                            await using var iconStream = await ((IGame)loadout.Installation.Game).Icon.GetStreamAsync();

                            var vm = serviceProvider.GetRequiredService<IImageButtonViewModel>();
                            vm.Name = loadout.Name;
                            vm.Image = LoadImageFromStream(iconStream);
                            vm.IsActive = false;
                            vm.WorkspaceContext = new LoadoutContext { LoadoutId = loadoutId };
                            vm.Click = ReactiveCommand.Create(() => ChangeToLoadoutWorkspace(loadoutId));
                            return vm;
                        }
                    )
                    .OnUI()
                    .Bind(out _loadoutSpineItems)
                    .SubscribeWithErrorLogging()
                    .DisposeWith(disposables);

                // Create Left Menus for each workspace on demand
                workspaceController.AllWorkspaces
                    .ToObservableChangeSet()
                    .Transform(workspace =>
                        {
                            var leftMenu = workspaceAttachmentsFactory.CreateLeftMenuFor(
                                workspace.Context,
                                workspace.Id,
                                workspaceController
                            );
                            // This should never be null, since there should be a factory for each context type, but in case
                            return leftMenu ?? new EmptyLeftMenuViewModel(workspace.Id);
                        }
                    )
                    .Bind(out _leftMenus)
                    .SubscribeWithErrorLogging()
                    .DisposeWith(disposables);

                // Navigate away from the Loadout workspace if the Loadout is removed
                loadoutRegistry.LoadoutRootChanges
                    .OnUI()
                    .OnItemRemoved(loadoutId =>
                    {
                        if (workspaceController.ActiveWorkspace?.Context is LoadoutContext activeLoadoutContext &&
                            activeLoadoutContext.LoadoutId == loadoutId)
                        {
                            workspaceController.ChangeOrCreateWorkspaceByContext<HomeContext>(() => new PageData
                            {
                                FactoryId = MyGamesPageFactory.StaticId,
                                Context = new MyGamesPageContext(),
                            });
                        }
                    }, false)
                    .SubscribeWithErrorLogging()
                    .DisposeWith(disposables);

                // Update the LeftMenuViewModel when the active workspace changes
                workspaceController.WhenAnyValue(controller => controller.ActiveWorkspace)
                    .Select(workspace => workspace?.Id)
                    .Select(workspaceId => _leftMenus.FirstOrDefault(menu => menu.WorkspaceId == workspaceId))
                    .BindToVM(this, vm => vm.LeftMenuViewModel)
                    .DisposeWith(disposables);



                // Update the active spine item when the active workspace changes
                workspaceController
                    .WhenAnyValue(controller => controller.ActiveWorkspace)
                    .Select(workspace => workspace?.Context)
                    .WhereNotNull()
                    .SubscribeWithErrorLogging(context =>
                        {
                            var itemToActivate = _specialSpineItems
                                .Concat(_loadoutSpineItems)
                                .FirstOrDefault(spineItem => spineItem.WorkspaceContext?.Equals(context) == true);

                            if (itemToActivate == null)
                                return;

                            if (_activeSpineItem != null)
                                _activeSpineItem.IsActive = false;

                            itemToActivate.IsActive = true;
                            _activeSpineItem = itemToActivate;
                        }
                    )
                    .DisposeWith(disposables);

                // Update the active spine item if the loadoutList changes
                _loadoutSpineItems.ToObservableChangeSet()
                    .SubscribeWithErrorLogging(_ =>
                        {
                            if (_activeSpineItem is not { WorkspaceContext: LoadoutContext loadoutContext }) return;

                            // The spine item might have been replaced with a new one (TransformAsync)
                            var newSpineItem = _loadoutSpineItems.FirstOrDefault(
                                spineItem => loadoutContext.Equals(spineItem.WorkspaceContext)
                            );
                            if (newSpineItem == null) return;

                            _activeSpineItem.IsActive = false;
                            newSpineItem.IsActive = true;
                            _activeSpineItem = newSpineItem;
                        }
                    )
                    .DisposeWith(disposables);
            }
        );
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
        if (!_windowManager.TryGetActiveWindow(out var window)) return;
        var workspaceController = window.WorkspaceController;

        workspaceController.ChangeOrCreateWorkspaceByContext<HomeContext>(() => new PageData
            {
                FactoryId = MyGamesPageFactory.StaticId,
                Context = new MyGamesPageContext()
            }
        );
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
            () => new LoadoutContext
            {
                LoadoutId = loadoutId
            }
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
            }
        );
    }
}
