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
using NexusMods.Abstractions.Loadouts.Ids;
using NexusMods.Abstractions.MnemonicDB.Attributes;
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

    private ReadOnlyObservableCollection<IImageButtonViewModel> _loadoutSpineItems = new([]);
    public ReadOnlyObservableCollection<IImageButtonViewModel> LoadoutSpineItems => _loadoutSpineItems;
    public IIconButtonViewModel Home { get; }
    public ISpineDownloadButtonViewModel Downloads { get; }
    private IList<ISpineItemViewModel> _specialSpineItems = new List<ISpineItemViewModel>();

    private ISpineItemViewModel? _activeSpineItem;

    private ReadOnlyObservableCollection<ILeftMenuViewModel> _leftMenus = new([]);
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
        IWorkspaceAttachmentsFactoryManager workspaceAttachmentsFactory,
        IRepository<Loadout.Model> loadoutRepository)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _windowManager = windowManager;
        _conn = conn;

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
                loadoutRepository.Observable
                    .ToObservableChangeSet()
                    .Filter(loadout => loadout.IsVisible())
                    .TransformAsync(async loadout =>
                        {
                            
                            await using var iconStream = await ((IGame)loadout.Installation.Game).Icon.GetStreamAsync();

                            var vm = serviceProvider.GetRequiredService<IImageButtonViewModel>();
                            vm.Name = loadout.Name;
                            vm.Image = LoadImageFromStream(iconStream);
                            vm.IsActive = false;
                            vm.WorkspaceContext = new LoadoutContext { LoadoutId = loadout.LoadoutId };
                            vm.Click = ReactiveCommand.Create(() => ChangeToLoadoutWorkspace(loadout.LoadoutId));
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
                        try
                        {
                            var leftMenu = workspaceAttachmentsFactory.CreateLeftMenuFor(
                                workspace.Context,
                                workspace.Id,
                                workspaceController
                            );

                            return leftMenu ?? new EmptyLeftMenuViewModel(workspace.Id, message: $"Missing {workspace.Context.GetType()}");
                        }
                        catch (Exception e)
                        {
                            _logger.LogError(e, "Exception while creating left menu for context {Context}", workspace.Context);
                            return new EmptyLeftMenuViewModel(workspace.Id, message: $"Error for {workspace.Context.GetType()}");
                        }
                    })
                    .Bind(out _leftMenus)
                    .SubscribeWithErrorLogging()
                    .DisposeWith(disposables);

                // Navigate away from the Loadout workspace if the Loadout is removed
                loadoutRepository.Observable
                    .ToObservableChangeSet()
                    .OnUI()
                    .OnItemRemoved(loadout =>
                    {
                        if (workspaceController.ActiveWorkspace?.Context is LoadoutContext activeLoadoutContext &&
                            activeLoadoutContext.LoadoutId == loadout.LoadoutId)
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
