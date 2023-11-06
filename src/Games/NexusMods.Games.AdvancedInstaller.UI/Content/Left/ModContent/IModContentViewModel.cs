namespace NexusMods.Games.AdvancedInstaller.UI.Content.Left;

public interface IModContentViewModel : IViewModelInterface
{
    /// <summary>
    ///     The ViewModel containing the tree data.
    /// </summary>
    ITreeEntryViewModel Root { get; }

    public HierarchicalTreeDataGridSource<ITreeEntryViewModel> Tree { get; }
}
