using JetBrains.Annotations;
using NexusMods.App.UI.Controls.Trees.Common;
using NexusMods.Sdk.Games;

namespace NexusMods.App.UI.Controls.Trees.Files;

public class FileTreeNodeDesignViewModel : FileTreeNodeViewModel, IFileTreeNodeViewModel
{
    [UsedImplicitly] // Via designer, if uncommented.
    public static FileTreeNodeDesignViewModel SampleFile { get; } = new(true, new GamePath(LocationId.Game, "Sample File"), 0);

    [UsedImplicitly] // Via designer, if uncommented.
    public static FileTreeNodeDesignViewModel SampleFolder { get; } = new(false, new GamePath(LocationId.Game, "Sample Folder"), 0);

    public new string Name { get; }
    
    [UsedImplicitly] // By designer.
    public FileTreeNodeDesignViewModel() : this(true, new GamePath(LocationId.Game, ""), "Design Folder Name")
    {
    }
    
    public FileTreeNodeDesignViewModel(bool isFile, GamePath fullPath, ulong fileSize)
    {
        IsFile = isFile;
        Icon = IsFile ? fullPath.Extension.GetIconType() : FileTreeNodeIconType.ClosedFolder;
        Name = fullPath.Path.FileName;
        Key = fullPath;
        ParentKey = fullPath.Parent;
        FileSize = fileSize;
    }

    public FileTreeNodeDesignViewModel(bool isFile, GamePath fullPath, string name) : this(isFile, fullPath, 0)
    {
        Name = name;
        ParentKey = new GamePath(LocationId.Unknown, "");
    }
    
    public FileTreeNodeDesignViewModel(bool isFile, GamePath fullPath, string name, ulong fileSize, uint numFiles) : this(isFile, fullPath, fileSize)
    {
        Name = name;
        ParentKey = new GamePath(LocationId.Unknown, "");
    }
}
