using System.Reactive;
using NexusMods.Abstractions.GameLocators;
using ReactiveUI;

namespace NexusMods.App.UI.Controls.Trees.Files;

public interface IFileTreeNodeViewModel : IViewModelInterface
{
    /// <summary>
    ///     Returns true if this is a folder.
    /// </summary>
    bool IsFile { get; }

    /// <summary>
    ///     Name of the file or folder segment.
    /// </summary>
    string Name { get; }
    
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
