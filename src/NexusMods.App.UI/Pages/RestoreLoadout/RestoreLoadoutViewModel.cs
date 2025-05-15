using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using DynamicData;
using NexusMods.Abstractions.Loadouts;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.Cascade;
using NexusMods.DataModel.Undo;
using NexusMods.MnemonicDB.Abstractions;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Pages.RestoreLoadout;

public class RestoreLoadoutViewModel : APageViewModel<IRestoreLoadoutViewModel>, IRestoreLoadoutViewModel
{
    private readonly UndoService _undoService;
    private readonly IConnection _conn;

    
    private ReadOnlyObservableCollection<LoadoutRevisionWithStats> _revisions = new(new ObservableCollection<LoadoutRevisionWithStats>());
    public ReadOnlyObservableCollection<LoadoutRevisionWithStats> Revisions => _revisions;
    
    public RestoreLoadoutViewModel(IWindowManager windowManager, UndoService undoService, IConnection connection) : base(windowManager)
    {
        _conn = connection;
        _undoService = undoService;
        
        this.WhenActivated(d =>
        {
            _conn.Topology
                .Observe(_undoService.Revisions.Where(l => l.LoadoutId == LoadoutId.Value))
                .Bind(out _revisions)
                .Subscribe()
                .DisposeWith(d);
        });
    }
    
    [Reactive]
    public LoadoutId LoadoutId { get; set; }

}
