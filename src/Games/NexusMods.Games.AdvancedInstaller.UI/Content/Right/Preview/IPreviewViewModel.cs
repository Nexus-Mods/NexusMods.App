using System.Collections.ObjectModel;
using DynamicData;
using NexusMods.Paths;

namespace NexusMods.Games.AdvancedInstaller.UI.Preview;

public interface IPreviewViewModel : IViewModelInterface
{
    /// <summary>
    /// The TreeContainers to display in the preview.
    /// Each of the ContainerVM corresponds to a different <see cref="LocationId"/>.
    /// </summary>
    public ReadOnlyObservableCollection<ILocationPreviewTreeViewModel> TreeContainers { get; }

    public SourceCache<IPreviewTreeEntryViewModel, GamePath> TreeEntriesCache { get; }
    public ReadOnlyObservableCollection<TreeNodeVM<IPreviewTreeEntryViewModel, GamePath>> TreeRoots { get; }

}
