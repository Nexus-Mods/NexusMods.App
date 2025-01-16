using System.Collections.ObjectModel;
using JetBrains.Annotations;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.UI;

namespace Examples.TreeDataGrid.SingleColumn.FileColumn;

public class FileColumnDesignViewModel(bool isFile, GamePath fullPath) : AViewModel<IFileColumnViewModel>, IFileColumnViewModel
{
    [UsedImplicitly] // Via designer, if uncommented.
    public static FileColumnDesignViewModel SampleFile { get; } = new(true, new GamePath(LocationId.Game, "Sample File"));

    [UsedImplicitly] // Via designer, if uncommented.
    public static FileColumnDesignViewModel SampleFolder { get; } = new(false, new GamePath(LocationId.Game, "Sample Folder"));

    public bool IsFile { get; set; } = isFile;
    public string Name { get; } = fullPath.Path.FileName;
    public GamePath Key { get; set; } = fullPath;
    public bool IsExpanded { get; set; }
    public ReadOnlyObservableCollection<IFileColumnViewModel>? Children { get; set; } = null;
    public IFileColumnViewModel? Parent { get; set; } = null;
    
    [UsedImplicitly] // Designer
    public FileColumnDesignViewModel() : this(true, new GamePath(LocationId.Game, ""), "Design Folder Name") { }

    public FileColumnDesignViewModel(bool isFile, GamePath fullPath, string name) : this(isFile, fullPath)
    {
        Name = name;
    }
}
