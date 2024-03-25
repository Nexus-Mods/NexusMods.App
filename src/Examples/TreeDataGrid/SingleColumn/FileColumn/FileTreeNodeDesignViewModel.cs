using JetBrains.Annotations;
using NexusMods.Abstractions.GameLocators;
using NexusMods.App.UI;
using ReactiveUI.Fody.Helpers;

namespace Examples.TreeDataGrid.SingleColumn.FileColumn;

public class FileTreeNodeDesignViewModel : AViewModel<IFileTreeNodeViewModel>, IFileTreeNodeViewModel
{
    [UsedImplicitly] // Via designer, if uncommented.
    public static FileTreeNodeDesignViewModel SampleFile { get; } = new(true, new GamePath(LocationId.Game, "Sample File"));

    [UsedImplicitly] // Via designer, if uncommented.
    public static FileTreeNodeDesignViewModel SampleFolder { get; } = new(false, new GamePath(LocationId.Game, "Sample Folder"));

    [Reactive]
    public FileTreeNodeIconType Icon { get; set; }
    public bool IsFile { get; set; }
    public string Name { get; }
    public GamePath Key { get; }
    public GamePath ParentKey { get; }
    public bool IsExpanded { get; set; }
    
    [UsedImplicitly] // Designer
    public FileTreeNodeDesignViewModel() : this(true, new GamePath(LocationId.Game, ""), "Design Folder Name")
    {
        
    }
    
    public FileTreeNodeDesignViewModel(bool isFile, GamePath fullPath)
    {
        IsFile = isFile;
        Icon = IsFile ? FileTreeNodeIconType.File : FileTreeNodeIconType.Folder;
        Name = fullPath.Path.FileName;
        Key = fullPath;
        ParentKey = fullPath.Parent;
    }

    public FileTreeNodeDesignViewModel(bool isFile, GamePath fullPath, string name) : this(isFile, fullPath)
    {
        Name = name;
        ParentKey = new GamePath(LocationId.Unknown, "");
    }
}
