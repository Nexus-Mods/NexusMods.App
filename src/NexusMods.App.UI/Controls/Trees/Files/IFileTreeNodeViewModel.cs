using System.Reactive;
using NexusMods.Abstractions.GameLocators;
using ReactiveUI;

namespace NexusMods.App.UI.Controls.Trees.Files;

public interface IFileTreeNodeViewModel : IViewModelInterface
{
    /// <summary>
    ///     The icon that's used to display this specific node.
    /// </summary>
    FileTreeNodeIconType Icon { get; }
    
    /// <summary>
    ///     Name of the file or folder segment.
    /// </summary>
    string Name { get; }
    
    /// <summary>
    ///     The size of the file, in bytes.
    /// </summary>
    long FileSize { get; }
    
    /// <summary>
    ///     The full path to this visible node.
    /// </summary>
    GamePath FullPath { get; }

    /// <summary>
    ///     The full path to this node's parent.
    /// </summary>
    GamePath ParentPath { get; }
    
    /// <summary>
    ///     The command used to begin 'viewing' a file.
    /// </summary>
    ReactiveCommand<Unit, Unit> ViewCommand { get; }
}

public enum FileTreeNodeIconType
{
    /// <summary>
    ///     Shows a regular 'file' icon.
    /// </summary>
    File,
    
    /// <summary>
    ///     Show a 'closed folder' icon.
    /// </summary>
    ClosedFolder,
    
    /// <summary>
    ///     Show an 'open folder' icon.
    /// </summary>
    OpenFolder,
}
