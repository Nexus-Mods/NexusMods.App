using NexusMods.Games.AdvancedInstaller.UI.Content.Left;
using NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.SelectLocation;

namespace NexusMods.Games.AdvancedInstaller.UI.Content;

/// <summary>
///     Interface for a component which facilitates the exchange of information between different AdvancedInstaller components.
/// </summary>
public interface IAdvancedInstallerCoordinator : IModContentUpdateReceiver, ISelectableDirectoryUpdateReceiver { }

/// <summary>
///     An interface for something that receives item selection updates from the <see cref="ModContentView"/>
/// </summary>
public interface IModContentUpdateReceiver
{
    /// <summary>
    ///     This item is called when an item is selected.
    /// </summary>
    /// <param name="treeEntryViewModel">The viewmodel of the tree entry being selected.</param>
    void OnSelect(ITreeEntryViewModel treeEntryViewModel);

    /// <summary>
    ///     This item is called when the last item's selection is cancelled.
    /// </summary>
    /// <param name="treeEntryViewModel">The viewmodel of the tree entry no longer being selected.</param>
    void OnCancelSelect(ITreeEntryViewModel treeEntryViewModel);

    /// <summary>
    ///     Retrieves the deployment data used.
    /// </summary>
    DeploymentData Data { get; }
}

/// <summary>
///     An interface for something that receives directory selection notifications from <see cref="SelectLocationView"/>
/// </summary>
public interface ISelectableDirectoryUpdateReceiver
{
    /// <summary>
    ///     This function is called when the user selects a directory from the directory selector.
    /// </summary>
    /// <param name="directory">The directory that was selected.</param>
    void OnDirectorySelected(Right.Results.SelectLocation.SelectableDirectoryEntry.ITreeEntryViewModel directory);
}
