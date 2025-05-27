using System.Reactive;
using NexusMods.Abstractions.UI;
using NexusMods.DataModel.Undo;
using NexusMods.MnemonicDB.Abstractions;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.RestoreLoadout;

public class RevisionDesignViewModel : AViewModel<IRevisionViewModel>, IRevisionViewModel
{
    public RevisionDesignViewModel(LoadoutRevisionWithStats revision)
    {
        Revision = revision;
        RestoreToCommand = ReactiveCommand.Create(() => { });
    }
    
    public ReactiveCommand<Unit, Unit> RestoreToCommand { get; }
    public LoadoutRevisionWithStats Revision { get; }
}
