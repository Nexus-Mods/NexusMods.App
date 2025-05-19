using System.Reactive;
using NexusMods.Abstractions.UI;
using NexusMods.DataModel.Undo;
using NexusMods.MnemonicDB.Abstractions;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.RestoreLoadout;

public class RevisionViewModel : AViewModel<IRevisionViewModel>, IRevisionViewModel
{
    public RevisionViewModel(LoadoutRevisionWithStats revision, UndoService undoService)
    {
        Revision = revision;
        RestoreToCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                await undoService.RevertTo(revision.LoadoutId, TxId.From(revision.TxId.Value));
            }
        );
    }
    
    public ReactiveCommand<Unit, Unit> RestoreToCommand { get; }
    public LoadoutRevisionWithStats Revision { get; }
}
