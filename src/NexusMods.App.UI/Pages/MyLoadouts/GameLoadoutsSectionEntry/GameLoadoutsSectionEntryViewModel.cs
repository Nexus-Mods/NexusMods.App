using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Threading;
using DynamicData;
using DynamicData.Binding;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Loadouts;
using NexusMods.App.UI.Controls.LoadoutCard;
using NexusMods.App.UI.Pages.LoadoutGrid;
using NexusMods.App.UI.Resources;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.MnemonicDB.Abstractions;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.MyLoadouts.GameLoadoutsSectionEntry;

public class GameLoadoutsSectionEntryViewModel : AViewModel<IGameLoadoutsSectionEntryViewModel>, IGameLoadoutsSectionEntryViewModel
{
    private static readonly CardViewModelComparer CardComparerInstance = new();
    private readonly CompositeDisposable _compositeDisposable;
    private readonly GameInstallation _gameInstallation;
    private readonly IWindowManager _windowManager;
    private readonly ReadOnlyObservableCollection<IViewModelInterface> _loadoutCardViewModels;
    private readonly ReadOnlyObservableCollection<IViewModelInterface> _cardViewModels;
    private readonly SourceList<IViewModelInterface> _cardViewModelsSourceList = new();
    public string HeadingText { get; }
    public ReadOnlyObservableCollection<IViewModelInterface> CardViewModels => _cardViewModels;


    public GameLoadoutsSectionEntryViewModel(GameInstallation gameInstallation, IConnection conn, IServiceProvider serviceProvider, IWindowManager windowManager)
    {
        _compositeDisposable = new CompositeDisposable();
        _gameInstallation = gameInstallation;
        _windowManager = windowManager;
        HeadingText = string.Format(Language.MyLoadoutsGameSectionHeading, _gameInstallation.Game.Name);

        Loadout.ObserveAll(conn)
            .Filter(l => l.IsVisible() && l.InstallationInstance.LocationsRegister[LocationId.Game] == _gameInstallation.LocationsRegister[LocationId.Game])
            .Transform(loadout =>
                {
                    return (IViewModelInterface)new LoadoutCardViewModel(loadout, conn, serviceProvider)
                    {
                        VisitLoadoutCommand = ReactiveCommand.Create(() => NavigateToLoadout(loadout)),
                        CloneLoadoutCommand = ReactiveCommand.Create(() =>
                            {
                                // TODO: Implement Loadout cloning
                            }
                        ),
                    };
                }
            )
            .Bind(out _loadoutCardViewModels)
            .Subscribe()
            .DisposeWith(_compositeDisposable);

        var createNewLoadoutCard = new CreateNewLoadoutCardViewModel()
        {
            AddLoadoutCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                await _gameInstallation.GetGame().SynchronizerOld.CreateLoadout(_gameInstallation);
            }),
        };

        _cardViewModelsSourceList.Add(createNewLoadoutCard);

        _cardViewModelsSourceList.Connect()
            .Merge(_loadoutCardViewModels.ToObservableChangeSet())
            .Sort(CardComparerInstance)
            .OnUI()
            .Bind(out _cardViewModels)
            .SubscribeWithErrorLogging()
            .DisposeWith(_compositeDisposable);
    }

    private class CardViewModelComparer : IComparer<IViewModelInterface>
    {
        public int Compare(IViewModelInterface? x, IViewModelInterface? y)
        {
            if (x == null) return y == null ? 0 : -1;
            if (y == null) return 1;

            // Prioritize CreateNewLoadoutCardViewModel to always come first.
            if (x is CreateNewLoadoutCardViewModel) return -1;
            if (y is CreateNewLoadoutCardViewModel) return 1;

            return (x, y) switch
            {
                (LoadoutCardViewModel lx, LoadoutCardViewModel ly) => string.Compare(lx.LoadoutName, ly.LoadoutName, StringComparison.Ordinal),
                (SkeletonLoadoutCardViewModel sx, SkeletonLoadoutCardViewModel sy) => string.Compare(sx.LoadoutName, sy.LoadoutName, StringComparison.Ordinal),
                (LoadoutCardViewModel lxs, SkeletonLoadoutCardViewModel sys) => string.Compare(lxs.LoadoutName, sys.LoadoutName, StringComparison.Ordinal),
                (SkeletonLoadoutCardViewModel sxs, LoadoutCardViewModel lys) => string.Compare(sxs.LoadoutName, lys.LoadoutName, StringComparison.Ordinal),
                _ => 0,
            };
        }
    }


    private void NavigateToLoadout(Loadout.ReadOnly loadout)
    {
        var loadoutId = loadout.LoadoutId;
        Dispatcher.UIThread.Invoke(() =>
            {
                var workspaceController = _windowManager.ActiveWorkspaceController;

                workspaceController.ChangeOrCreateWorkspaceByContext(
                    context => context.LoadoutId == loadoutId,
                    () => new PageData
                    {
                        FactoryId = LoadoutGridPageFactory.StaticId,
                        Context = new LoadoutGridContext
                        {
                            LoadoutId = loadoutId,
                        },
                    },
                    () => new LoadoutContext
                    {
                        LoadoutId = loadoutId,
                    }
                );
            }
        );
    }

    public void Dispose()
    {
        _compositeDisposable.Dispose();
    }
}
