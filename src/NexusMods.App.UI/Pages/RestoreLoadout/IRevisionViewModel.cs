using System.Reactive;
using NexusMods.Abstractions.UI;
using NexusMods.DataModel.Undo;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.RestoreLoadout;

public interface IRevisionViewModel : IViewModelInterface
{
    public ReactiveCommand<Unit, Unit> RestoreToCommand { get; }
    public LoadoutRevisionWithStats Revision { get; }
}
