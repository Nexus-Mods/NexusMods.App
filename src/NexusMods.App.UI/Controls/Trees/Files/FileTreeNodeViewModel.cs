using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games.Trees;
using NexusMods.Paths;
using NexusMods.Paths.Trees;
using NexusMods.Paths.Trees.Traits;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Controls.Trees.Files;

public class FileTreeNodeViewModel<TValue> : AViewModel<IFileTreeNodeViewModel>, IFileTreeNodeViewModel
{
    private readonly KeyedBox<RelativePath, GamePathNode<TValue>> _item;
    
    [Reactive]
    public FileTreeNodeIconType Icon { get; set; }

    public bool IsFile => _item.Item.IsFile;
    public string Name => _item.Item.Segment;
    public ulong FileSize { get; }
    public GamePath Key => _item.GamePath();
    public GamePath ParentKey => _item.Parent()!.GamePath();

    public FileTreeNodeViewModel(KeyedBox<RelativePath, GamePathNode<TValue>> item, ulong fileSize)
    {
        _item = item;
        FileSize = fileSize;
        Icon = _item.Item.IsFile ? FileTreeNodeIconType.File : FileTreeNodeIconType.ClosedFolder;
    }
}
