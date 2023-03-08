using System.Collections.ObjectModel;
using NexusMods.App.UI.Controls.GameWidget;

namespace NexusMods.App.UI.RightContent;

public interface IFoundGamesViewModel : IRightContent
{
    public ReadOnlyObservableCollection<IGameWidgetViewModel> Games { get; }
}
