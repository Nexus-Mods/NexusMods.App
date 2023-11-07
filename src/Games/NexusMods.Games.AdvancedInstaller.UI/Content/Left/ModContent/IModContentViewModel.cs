using NexusMods.Games.AdvancedInstaller.UI.ModContent;

namespace NexusMods.Games.AdvancedInstaller.UI.Content.Left;

public interface IModContentViewModel : IViewModelInterface
{
    /// <summary>
    ///     The ViewModel containing the tree data.
    /// </summary>
    IModContentTreeEntryViewModel Root { get; }

    public HierarchicalTreeDataGridSource<IModContentTreeEntryViewModel> Tree { get; }
}
