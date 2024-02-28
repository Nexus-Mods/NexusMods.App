using System.Reactive;
using JetBrains.Annotations;
using NexusMods.Abstractions.GameLocators;
using ReactiveUI;

namespace NexusMods.App.UI.Controls.Trees.Files;

public class FileTreeNodeDesignViewModel : AViewModel<IFileTreeNodeViewModel>, IFileTreeNodeViewModel
{
    [UsedImplicitly] // Via designer, if uncommented.
    public static FileTreeNodeDesignViewModel SampleFile { get; } = new(true, new GamePath(LocationId.Game, "Sample File"));

    [UsedImplicitly] // Via designer, if uncommented.
    public static FileTreeNodeDesignViewModel SampleFolder { get; } = new(false, new GamePath(LocationId.Game, "Sample Folder"));
    
    public bool IsFile { get; }
    public string Name { get; }
    public GamePath FullPath { get; set; } = default;
    public GamePath ParentPath { get; set; } = default;
    public ReactiveCommand<Unit, Unit> ViewCommand { get; } = ReactiveCommand.Create(() => {});

    public FileTreeNodeDesignViewModel(bool isFile, GamePath fullPath)
    {
        IsFile = isFile;
        Name = fullPath.Path.FileName;
        FullPath = fullPath;
        ParentPath = fullPath.Parent;
    }

    public FileTreeNodeDesignViewModel(bool isFile, GamePath fullPath, string name) : this(isFile, fullPath)
    {
        Name = name;
        ParentPath = default(GamePath);
    }
}
