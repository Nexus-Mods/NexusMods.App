using System.Reactive;
using NexusMods.Abstractions.UI;
using ReactiveUI;

namespace NexusMods.App.UI.Controls.LoadoutCard;

public interface ICreateNewLoadoutCardViewModel : IViewModelInterface
{
    ReactiveCommand<Unit, Unit> AddLoadoutCommand { get; }
}
