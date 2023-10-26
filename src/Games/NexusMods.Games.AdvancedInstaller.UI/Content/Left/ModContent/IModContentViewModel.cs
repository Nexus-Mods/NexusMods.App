namespace NexusMods.Games.AdvancedInstaller.UI.Content.Left;

public interface IModContentViewModel : IViewModel
{
    /// <summary>
    ///     The ViewModel containing the tree data.
    /// </summary>
    ITreeEntryViewModel Root { get; }

    /// <summary>
    ///     An item to receive updates from the <see cref="ModContentView"/>.
    /// </summary>
    IModContentUpdateReceiver Receiver { get; }

    public HierarchicalTreeDataGridSource<ITreeEntryViewModel> Tree { get; }
}
