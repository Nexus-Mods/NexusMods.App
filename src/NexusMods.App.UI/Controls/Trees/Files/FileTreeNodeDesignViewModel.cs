using JetBrains.Annotations;
using NexusMods.Abstractions.GameLocators;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Controls.Trees.Files;

public class FileTreeNodeDesignViewModel : AViewModel<IFileTreeNodeViewModel>, IFileTreeNodeViewModel
{
    [UsedImplicitly] // Via designer, if uncommented.
    public static FileTreeNodeDesignViewModel SampleFile { get; } = new(true, new GamePath(LocationId.Game, "Sample File"), -1);

    [UsedImplicitly] // Via designer, if uncommented.
    public static FileTreeNodeDesignViewModel SampleFolder { get; } = new(false, new GamePath(LocationId.Game, "Sample Folder"), -1);

    [Reactive]
    public FileTreeNodeIconType Icon { get; set; }
    public bool IsFile { get; }
    public string Name { get; }
    public long FileSize { get; }
    public GamePath FullPath { get; set; } = default;
    public GamePath ParentPath { get; set; } = default;
    
    public FileTreeNodeDesignViewModel() : this(true, new GamePath(LocationId.Game, ""), "Design Folder Name")
    {
        
    }
    
    public FileTreeNodeDesignViewModel(bool isFile, GamePath fullPath, long fileSize)
    {
        IsFile = isFile;
        Icon = IsFile ? FileTreeNodeIconType.File : FileTreeNodeIconType.ClosedFolder;
        Name = fullPath.Path.FileName;
        FullPath = fullPath;
        ParentPath = fullPath.Parent;
        FileSize = fileSize;
    }

    public FileTreeNodeDesignViewModel(bool isFile, GamePath fullPath, string name) : this(isFile, fullPath, -1)
    {
        Name = name;
        ParentPath = default(GamePath);
    }
}
