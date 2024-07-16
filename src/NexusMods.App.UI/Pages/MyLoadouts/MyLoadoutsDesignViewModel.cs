using System.Collections.ObjectModel;
using NexusMods.App.UI.Pages.MyLoadouts.GameLoadoutsSectionEntry;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;

namespace NexusMods.App.UI.Pages.MyLoadouts;

public class MyLoadoutsDesignViewModel : APageViewModel<IMyLoadoutsViewModel>, IMyLoadoutsViewModel
{

    public MyLoadoutsDesignViewModel() : base(new DesignWindowManager())
    {
    }

    public MyLoadoutsDesignViewModel(IWindowManager windowManager) : base(windowManager)
    {
    }

    public ReadOnlyObservableCollection<IGameLoadoutsSectionEntryViewModel> GameSectionVMs { get; } = new([
            new GameLoadoutsSectionEntryDesignViewModel(),
            new GameLoadoutsSectionEntryDesignViewModel(),
        ]
    );
}
