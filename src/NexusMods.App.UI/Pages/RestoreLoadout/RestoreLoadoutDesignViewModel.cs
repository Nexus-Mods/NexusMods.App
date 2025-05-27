using System.Collections.ObjectModel;
using NexusMods.Abstractions.Loadouts;
using NexusMods.App.UI.Pages.RestoreLoadout;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.DataModel.Undo;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.App.UI.Pages.RestoreLoadout;

    public class RestoreLoadoutDesignViewModel : APageViewModel<IRestoreLoadoutViewModel>, IRestoreLoadoutViewModel
    {
        public RestoreLoadoutDesignViewModel() : base(new DesignWindowManager())
        {
            Revisions = new ReadOnlyObservableCollection<IRevisionViewModel>(new ObservableCollection<IRevisionViewModel>
            {
                new RevisionDesignViewModel(new LoadoutRevisionWithStats(EntityId.From(123), EntityId.From(456), 1,2,3,4,  DateTimeOffset.Now )),
                new RevisionDesignViewModel(new LoadoutRevisionWithStats(EntityId.From(789), EntityId.From(456), 1,2,3,4,  DateTimeOffset.Now.AddDays(-1))),
                new RevisionDesignViewModel(new LoadoutRevisionWithStats(EntityId.From(101112), EntityId.From(456), 1,2,3,4,  DateTimeOffset.Now.AddDays(-2))),
            });
        }
        
        public LoadoutId LoadoutId { get; set; } = LoadoutId.From(987);
        public ReadOnlyObservableCollection<IRevisionViewModel> Revisions { get; }
    }
