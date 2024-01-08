using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using Avalonia.Media.Imaging;
using DynamicData;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.App.UI.Controls.Spine.Buttons.Download;
using NexusMods.App.UI.Controls.Spine.Buttons.Icon;
using NexusMods.App.UI.Controls.Spine.Buttons.Image;
using NexusMods.App.UI.LeftMenu;
using NexusMods.App.UI.LeftMenu.Downloads;
using NexusMods.App.UI.LeftMenu.Game;
using NexusMods.App.UI.LeftMenu.Home;
using NexusMods.App.UI.Routing;
using NexusMods.App.UI.Routing.Messages;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.Loadouts;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Controls.Spine;

public class SpineViewModel : AViewModel<ISpineViewModel>, ISpineViewModel
{
    public IIconButtonViewModel Home { get; }

    public IIconButtonViewModel Add { get; }

    public IDownloadButtonViewModel Downloads { get; }

    private ReadOnlyObservableCollection<IImageButtonViewModel> _games =
        Initializers.ReadOnlyObservableCollection<IImageButtonViewModel>();
    public ReadOnlyObservableCollection<IImageButtonViewModel> Games => _games;

    public Subject<SpineButtonAction> Activations { get; } = new();

    [Reactive]
    public ILeftMenuViewModel LeftMenu { get; set; } =
        Initializers.ILeftMenuViewModel;

    private readonly Subject<SpineButtonAction> _actions = new();
    private readonly ILogger<SpineViewModel> _logger;
    private readonly IGameLeftMenuViewModel _gameLeftMenuViewModel;
    private readonly IHomeLeftMenuViewModel _homeLeftMenuViewModel;
    private readonly IDownloadsViewModel _downloadsViewModel;
    public IObservable<SpineButtonAction> Actions => _actions;

    public SpineViewModel(ILogger<SpineViewModel> logger,
        LoadoutRegistry loadoutRegistry,
        IDataStore dataStore,
        IIconButtonViewModel addButtonViewModel,
        IIconButtonViewModel homeButtonViewModel,
        IDownloadButtonViewModel downloadsButtonViewModel,
        IDownloadsViewModel downloadsViewModel,
        IHomeLeftMenuViewModel homeLeftMenuViewModel,
        IGameLeftMenuViewModel gameLeftMenuViewModel,
        IRouter router,
        IServiceProvider provider)
    {
        _logger = logger;

        Home = homeButtonViewModel;
        Add = addButtonViewModel;
        Downloads = downloadsButtonViewModel;

        _homeLeftMenuViewModel = homeLeftMenuViewModel;
        _downloadsViewModel = downloadsViewModel;
        _gameLeftMenuViewModel = gameLeftMenuViewModel;


        this.WhenActivated(disposables =>
        {
            router.Messages
                .OnUI()
                .SubscribeWithErrorLogging(logger, HandleMessage)
                .DisposeWith(disposables);

            loadoutRegistry.Games
                .Transform(game =>
                {
                    using var iconStream = game.Icon.GetStreamAsync().Result;
                    var vm = provider.GetRequiredService<IImageButtonViewModel>();
                    vm.Name = game.Name;
                    vm.Image = LoadImageFromStream(iconStream);
                    vm.IsActive = false;
                    vm.Tag = game;
                    vm.Click = ReactiveCommand.Create(() => NavigateToGame(game));
                    return vm;
                })
                .OnUI()
                .Bind(out _games)
                .SubscribeWithErrorLogging(logger)
                .DisposeWith(disposables);

            Home.Click = ReactiveCommand.Create(NavigateToHome);

            Add.Click = ReactiveCommand.Create(NavigateToAdd);

            Downloads.Click = ReactiveCommand.Create(NavigateToDownloads);

            Activations
                .SubscribeWithErrorLogging(logger, HandleActivation)
                .DisposeWith(disposables);

            // For now just select home on startup
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
        _logger.LogTrace("Home selected");
        _actions.OnNext(new SpineButtonAction(Type.Home));
        LeftMenu = _homeLeftMenuViewModel;
    }

    private void NavigateToAdd()
    {
        _logger.LogTrace("Add selected");
        _actions.OnNext(new SpineButtonAction(Type.Add));
    }
    private void NavigateToGame(IGame game)
    {
        _logger.LogTrace("Game {Game} selected", game);
        _actions.OnNext(new SpineButtonAction(Type.Game, game));
        _gameLeftMenuViewModel.Game = game;
        LeftMenu = _gameLeftMenuViewModel;
    }

    private void NavigateToDownloads()
    {
        _logger.LogTrace("Downloads selected");
        _actions.OnNext(new SpineButtonAction(Type.Download));
        LeftMenu = _downloadsViewModel;
    }

    private void HandleMessage(IRoutingMessage message)
    {
        switch (message)
        {
            case NavigateToLoadout navigateToLoadout:
                NavigateToGame(navigateToLoadout.Game);
                break;
            case NavigateToDownloads _:
                NavigateToDownloads();
                break;
        }
    }

    private void HandleActivation(SpineButtonAction action)
    {
        _logger.LogTrace("Activation {Action}", action);
        switch (action.Type)
        {
            case Type.Game:
            {
                Home.IsActive = false;
                Add.IsActive = false;
                Downloads.IsActive = false;
                foreach (var game in Games)
                {
                    game.IsActive = ReferenceEquals(game.Tag, action.Game);
                }

                break;
            }
            case Type.Download:
            {
                Home.IsActive = false;
                Add.IsActive = false;
                Downloads.IsActive = true;
                foreach (var game in Games)
                {
                    game.IsActive = false;
                }
                break;
            }
            case Type.Home:
            case Type.Add:
            default:
            {
                Home.IsActive = action.Type == Type.Home;
                Add.IsActive = action.Type == Type.Add;
                Downloads.IsActive = false;
                foreach (var game in Games)
                {
                    game.IsActive = false;
                }

                break;
            }
        }
    }
}
