using System.Collections.ObjectModel;
using JetBrains.Annotations;
using NexusMods.Abstractions.GameLocators;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Controls.Trees.Files;

public class FileTreeNodeDesignViewModel : AViewModel<IFileTreeNodeViewModel>, IFileTreeNodeViewModel
{
    [UsedImplicitly] // Via designer, if uncommented.
    public static FileTreeNodeDesignViewModel SampleFile { get; } = new(true, new GamePath(LocationId.Game, "Sample File"), 0);

    [UsedImplicitly] // Via designer, if uncommented.
    public static FileTreeNodeDesignViewModel SampleFolder { get; } = new(false, new GamePath(LocationId.Game, "Sample Folder"), 0);

    [Reactive]
    public FileTreeNodeIconType Icon { get; set; }
    public bool IsFile { get; }
    public string Name { get; }
    public ulong FileSize { get; }
    public GamePath Key { get; set; }
    public GamePath ParentKey { get; }
    public bool IsExpanded { get; set; }
    public ReadOnlyObservableCollection<IFileTreeNodeViewModel>? Children { get; set; }
    public IFileTreeNodeViewModel? Parent { get; set; }
    
    public FileTreeNodeDesignViewModel() : this(true, new GamePath(LocationId.Game, ""), "Design Folder Name")
    {
        
    }
    
    public FileTreeNodeDesignViewModel(bool isFile, GamePath fullPath, ulong fileSize)
    {
        IsFile = isFile;
        Icon = IsFile ? FileTreeNodeIconType.File : FileTreeNodeIconType.ClosedFolder;
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
    
    public FileTreeNodeDesignViewModel(bool isFile, GamePath fullPath, string name, ulong fileSize) : this(isFile, fullPath, fileSize)
    {
        Name = name;
        ParentKey = new GamePath(LocationId.Unknown, "");
    }
}
