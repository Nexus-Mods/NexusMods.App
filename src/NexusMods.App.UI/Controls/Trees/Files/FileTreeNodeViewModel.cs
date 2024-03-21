using System.Collections.ObjectModel;
using System.Diagnostics;
using NexusMods.Abstractions.GameLocators;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Controls.Trees.Files;

[DebuggerDisplay("Key = {Key.ToString()}, ParentKey = {ParentKey.ToString()}")]
public class FileTreeNodeViewModel : AViewModel<IFileTreeNodeViewModel>, IFileTreeNodeViewModel
{
    [Reactive]
    public FileTreeNodeIconType Icon { get; set; }

    public bool IsFile { get; }
    public string Name => Key.FileName;
    public ulong FileSize { get; }
    public GamePath Key { get; set; }
    public GamePath ParentKey { get; }
    [Reactive] public bool IsExpanded { get; set; }
    public ReadOnlyObservableCollection<IFileTreeNodeViewModel>? Children { get; set; }
    public IFileTreeNodeViewModel? Parent { get; set; }

    public FileTreeNodeViewModel(GamePath fullPath, GamePath parentPath, bool isFile, ulong fileSize)
    {
        Key = fullPath;
        ParentKey = parentPath;
        FileSize = fileSize;
        IsFile = isFile;
        Icon = isFile ? FileTreeNodeIconType.File : FileTreeNodeIconType.ClosedFolder;
    }
}
