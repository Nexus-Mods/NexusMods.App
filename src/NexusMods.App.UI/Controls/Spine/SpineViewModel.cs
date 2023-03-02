using System.Collections.ObjectModel;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Avalonia.Controls.Mixins;
using Avalonia.Media.Imaging;
using DynamicData;
using Microsoft.Extensions.Logging;
using NexusMods.App.UI.Controls.Spine.Buttons;
using NexusMods.App.UI.ViewModels;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Loadouts;
using ReactiveUI;

namespace NexusMods.App.UI.Controls.Spine;

public class SpineViewModel : AViewModel<ISpineViewModel>, ISpineViewModel
{
    private readonly IDataStore _dataStore;

    public HomeButtonViewModel Home { get; }

    public AddButtonViewModel Add { get; }

    private ReadOnlyObservableCollection<GameViewModel> _game;
    public ReadOnlyObservableCollection<GameViewModel> Games => _game;

    public Subject<SpineButtonAction> Activations { get; } = new();

    private readonly Subject<SpineButtonAction> _actions = new();
    private readonly ILogger<SpineViewModel> _logger;
    public IObservable<SpineButtonAction> Actions => _actions;

    public SpineViewModel(ILogger<SpineViewModel> logger, IDataStore dataStore, AddButtonViewModel addButtonViewModel, HomeButtonViewModel homebuttonViewModel)
    {
        _logger = logger;
        _dataStore = dataStore;

        Home = homebuttonViewModel;
        Add = addButtonViewModel;

        this.WhenActivated(disposables =>
        {
            _dataStore.RootChanges
                .Where(c => c.To.Category == EntityCategory.Loadouts)
                .Select(c => c.To)
                .Select(id => _dataStore.Get<LoadoutRegistry>(id, true))
                .StartWith(_dataStore.Get<LoadoutRegistry>(_dataStore.GetRoot(RootType.Loadouts) ?? IdEmpty.Empty, true))
                .Where(registry => registry != null)
                .SelectMany(registry => registry.Lists.Select(lst => lst.Value.Installation.Game).Distinct())
                .ToObservableChangeSet(x => x.Domain)
                .Transform(game =>
                {
                    using var iconStream = game.Icon.GetStream().Result;
                    return new GameViewModel
                    {
                        Name = game.Name,
                        Image = Bitmap.DecodeToWidth(iconStream, 48),
                        IsActive = false,
                        Tag = game,
                        Click = ReactiveCommand.Create(() =>
                        {
                            _logger.LogTrace("Game {Game} selected", game);
                            _actions.OnNext(new SpineButtonAction(Type.Game, game));
                        })
                    };
                })
                .Bind(out _game)
                .Subscribe()
                .DisposeWith(disposables);

            Home.Click = ReactiveCommand.Create(() =>
            {
                _logger.LogTrace("Home selected");
                _actions.OnNext(new SpineButtonAction(Type.Home));
            });

            Add.Click = ReactiveCommand.Create(() =>
            {
                _logger.LogTrace("Add selected");
                _actions.OnNext(new SpineButtonAction(Type.Add));
            });

            Activations.Subscribe(HandleActivation)
                .DisposeWith(disposables);
        });
    }

    private void HandleActivation(SpineButtonAction action)
    {
        _logger.LogTrace("Activation {Action}", action);
        if (action.Type == Type.Game)
        {
            Home.IsActive = false;
            Add.IsActive = false;
            foreach (var game in Games)
            {
                game.IsActive = ReferenceEquals(game.Tag, action.Game);
            }
        }
        else
        {
            Home.IsActive = action.Type == Type.Home;
            Add.IsActive = action.Type == Type.Add;
            foreach (var game in Games)
            {
                game.IsActive = false;
            }
        }
    }
}
