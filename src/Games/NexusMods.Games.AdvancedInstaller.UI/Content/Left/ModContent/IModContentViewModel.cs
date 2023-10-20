using Avalonia.Controls;
using NexusMods.App.UI;

namespace NexusMods.Games.AdvancedInstaller.UI.Content.Left;

public interface IModContentViewModel : IViewModel
{
    /// <summary>
    /// The ViewModel containing the tree data.
    /// </summary>
    ITreeEntryViewModel TreeVm { get; }

    public HierarchicalTreeDataGridSource<ITreeEntryViewModel> Tree { get; }
}
