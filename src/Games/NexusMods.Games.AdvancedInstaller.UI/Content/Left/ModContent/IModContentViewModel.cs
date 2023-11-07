namespace NexusMods.Games.AdvancedInstaller.UI.ModContent;

public interface IModContentViewModel : IViewModelInterface
{
    /// <summary>
    ///     The ViewModel containing the tree data.
    /// </summary>
    IModContentTreeEntryViewModel Root { get; }

    public HierarchicalTreeDataGridSource<IModContentTreeEntryViewModel> Tree { get; }
}
