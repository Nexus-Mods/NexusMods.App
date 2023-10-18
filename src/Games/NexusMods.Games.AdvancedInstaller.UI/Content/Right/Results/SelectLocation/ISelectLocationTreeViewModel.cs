using Avalonia.Controls;
using NexusMods.App.UI;

namespace NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.SelectLocation;

public interface ISelectLocationTreeViewModel : IViewModel
{
    public HierarchicalTreeDataGridSource<ITreeEntryViewModel> Tree { get; }

}
