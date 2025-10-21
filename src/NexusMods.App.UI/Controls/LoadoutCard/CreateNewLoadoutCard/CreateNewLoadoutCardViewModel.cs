using System.Reactive;
using NexusMods.UI.Sdk;
using ReactiveUI;

namespace NexusMods.App.UI.Controls.LoadoutCard;

public class CreateNewLoadoutCardViewModel : AViewModel<ICreateNewLoadoutCardViewModel>, ICreateNewLoadoutCardViewModel
{
    public required ReactiveCommand<Unit, Unit> AddLoadoutCommand { get; init; } 
}
