using System.Reactive;
using NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.PreviewView.PreviewEntry;
using NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.SelectLocation;
using NexusMods.Paths;
using NexusMods.Paths.FileTree;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.Games.AdvancedInstaller.UI.Content.Left;

/// <summary>
///     Represents an individual node in the 'Mod Content' section.
///     A node can represent any file or directory within the mod being unpacked during advanced install.
/// </summary>
/// <remarks>
///     Using this at runtime isn't exactly ideal given how many items there may be, but given everything is virtualized,
///     things should hopefully be a-ok!
/// </remarks>
public interface ITreeEntryViewModel : IUnlinkableItem
{
    /// <summary>
    ///     Status of the node in question.
    /// </summary>
    [Reactive]
    public ModContentNodeStatus Status { get; }

    /// <summary>
    ///     True if this is an element child of the root node.
    /// </summary>
    /// <remarks>
    ///     This is useful for the UI, e.g. to determine "Included" vs "Included with folder" text.
    /// </remarks>
    bool IsTopLevel { get; }

    /// <summary>
    ///     The name of this specific file in the tree.
    /// </summary>
    string FileName { get; }

    /// <summary>
    ///     The full relative path of this file in the tree.
    /// </summary>
    RelativePath FullPath { get; }

    /// <summary>
    ///     Name of the linked target which was created with <see cref="Link"/>.
    /// </summary>
    /// <remarks>
    ///     This is used such that we can unlink the entry on the left hand side.
    /// </remarks>
    IModContentBindingTarget? LinkedTarget { get; }

    /// <summary>
    ///     Contains the children nodes of this node.
    /// </summary>
    /// <remarks>
    ///     (Sewer) I got some notes to make here.
    ///
    ///     1. Lazy loading of this item should be investigated, in the case that the user has not yet expanded all
    ///        items yet.
    ///
    ///
    ///        When you map a folder, the state of all the children (recursively) must be updated;
    ///        meaning that the items (recursively) need to be loaded. Therefore, opportunities for lazy loading
    ///        are minimal.
    ///
    ///     2. The input collection from which the tree is constructed is immutable.
    ///
    ///        Mods cannot dynamically add files in the middle of the Advanced Installer
    ///        installation process. There is no need to use an observable collection here,
    ///        as that would just be unnecessary memory overhead.
    ///
    ///     Based on the above points, and given that the children count is already known in
    ///     <see cref="FileTreeNode{TPath,TValue}" />; an array is used, as it's the lowest
    ///     overhead collection available for the job.
    /// </remarks>
    ITreeEntryViewModel[] Children { get; }

    /// <summary>
    ///     True if this is the root node.
    /// </summary>
    bool IsRoot { get; }

    /// <summary>
    ///     True if this is a directory, in which case all files from child of this will be mapped to given
    ///     target folder.
    /// </summary>
    new bool IsDirectory { get; }

    /// <summary>
    ///     Binds the current node/source to the given target.
    /// </summary>
    /// <param name="data">The structure keeping track of deployment data.</param>
    /// <param name="target">
    ///     The target (directory) to receive the binding.
    ///     This is usually <see cref="TreeEntryViewModel"/>, care must be taken to ensure the target path matches the
    ///     correct path. To do this, search for the <see cref="FullPath"/> in root node/directory of <see cref="IModContentBindingTarget"/>.
    /// </param>
    /// <param name="targetAlreadyExisted">
    ///     Set this to true to indicate that this target has already existed.
    ///     i.e. The target is a non-user created folder.
    /// </param>
    void Link(DeploymentData data, IModContentBindingTarget target, bool targetAlreadyExisted);

    /// <summary>
    ///     The action executed when the user clicks 'Install' button.
    /// </summary>
    ReactiveCommand<Unit, Unit> BeginSelectCommand { get; }

    /// <summary>
    ///     The action executed when the user clicks the `Cancel` button after clicking the 'Install' button.
    /// </summary>
    ReactiveCommand<Unit, Unit> CancelSelectCommand { get; }

    /// <summary>
    ///     Removes itself and all of its children recursively from the deployment data.
    ///     This is executed when the user hits 'Remove' button from the left hand side.
    /// </summary>
    ReactiveCommand<DeploymentData, Unit> UnlinkCommand { get; }
}
