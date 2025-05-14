using System.Reactive.Linq;
using NexusMods.Abstractions.Loadouts;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.DataModel.Undo;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Pages.RestoreLoadout;

public class RestoreLoadoutViewModel : APageViewModel<IRestoreLoadoutViewModel>, IRestoreLoadoutViewModel
{
    private readonly UndoService _undoService;

    public RestoreLoadoutViewModel(IWindowManager windowManager, UndoService undoService) : base(windowManager)
    {
        _undoService = undoService;
        
        this.WhenActivated(d =>

        {
            ConnectQueries(LoadoutId);

        });
    }

    public async Task ConnectQueries(LoadoutId loadoutId)
    {
        var query = await _undoService.RevisionsFor(loadoutId);
        var obs = query.Observe()
    }


    [Reactive]
    public LoadoutId LoadoutId { get; set; }
}
