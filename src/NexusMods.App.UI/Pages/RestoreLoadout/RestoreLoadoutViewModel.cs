using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using DynamicData;
using NexusMods.Abstractions.Loadouts;
using NexusMods.App.UI.Resources;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.Cascade;
using NexusMods.DataModel.Undo;
using NexusMods.Icons;
using NexusMods.MnemonicDB.Abstractions;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Pages.RestoreLoadout;

public class RestoreLoadoutViewModel : APageViewModel<IRestoreLoadoutViewModel>, IRestoreLoadoutViewModel
{
    private readonly UndoService _undoService;
    private readonly IConnection _conn;

    
    private ReadOnlyObservableCollection<IRevisionViewModel> _revisions = new([]);
    public ReadOnlyObservableCollection<IRevisionViewModel> Revisions => _revisions;
    
    public RestoreLoadoutViewModel(IWindowManager windowManager, UndoService undoService, IConnection connection) : base(windowManager)
    {
        _conn = connection;
        _undoService = undoService;
        
        this.WhenActivated(d =>
        {
            _conn.Topology
                .Observe(_undoService.Revisions.Where(l => l.LoadoutId == LoadoutId.Value))
                .OnUI()
                .Transform(vm => (IRevisionViewModel)new RevisionViewModel(vm, _undoService))
                .Sort(Comparer<IRevisionViewModel>.Create((a, b) => a.Revision.TxId.CompareTo(b.Revision.TxId)))
                .Bind(out _revisions)
                .Subscribe()
                .DisposeWith(d);
        });
        
        TabIcon = IconValues.BackupRestore;
        TabTitle = Language.LoadoutLeftMenuViewModel_LoadoutLeftMenuViewModel_RestoreLoadout;
    }
    
    [Reactive]
    public LoadoutId LoadoutId { get; set; }

}
