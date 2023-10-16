using System.Collections.ObjectModel;
using NexusMods.App.UI;

namespace NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.PreviewView;

public interface IPreviewViewModel : IViewModel
{
    public ReadOnlyObservableCollection<ILocationPreviewTreeViewModel> Locations { get; }
}
