using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using DynamicData;
using NexusMods.Abstractions.Loadouts;
using NexusMods.App.UI.Pages.MyLoadouts.GameLoadoutsSectionEntry;
using NexusMods.App.UI.Resources;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.Icons;
using NexusMods.MnemonicDB.Abstractions;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.MyLoadouts;

public class MyLoadoutsViewModel : APageViewModel<IMyLoadoutsViewModel>, IMyLoadoutsViewModel
{
    private ReadOnlyObservableCollection<IGameLoadoutsSectionEntryViewModel> _gameSectionViewModels = new([]);
    private readonly IConnection _conn;
    
    public ReadOnlyObservableCollection<IGameLoadoutsSectionEntryViewModel> GameSectionViewModels => _gameSectionViewModels;

    public MyLoadoutsViewModel(
        IWindowManager windowManager,
        IConnection conn,
        IServiceProvider serviceProvider) : base(windowManager)
    {
        _conn = conn;
        TabTitle = Language.MyLoadoutsPageTitle;
        TabIcon = IconValues.ViewCarousel;
        
        this.WhenActivated(d =>
        {
            Loadout.ObserveAll(_conn)
                .Filter(l => l.IsVisible())
                .GroupOn(loadout => loadout.Installation.Path)
                .Transform(group => group.List.Items.First().InstallationInstance)
                .Transform(managedGameInstall =>
                    {
                        return (IGameLoadoutsSectionEntryViewModel) new GameLoadoutsSectionEntryViewModel(managedGameInstall, _conn, serviceProvider, windowManager);
                    }
                )
                .OnUI()
                .Bind(out _gameSectionViewModels)
                .SubscribeWithErrorLogging()
                .DisposeWith(d);
        });
    }

}
