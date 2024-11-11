using System.Collections.ObjectModel;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.UI;

namespace NexusMods.App.UI.Pages.LoadOrder.Prototype;

public interface ILoadOrderViewModel : IViewModelInterface
{
    ICollection<ISortableItemViewModel> SortableItems { get; }
}
