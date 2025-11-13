using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reactive.Disposables;
using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;
using Humanizer.Bytes;
using NexusMods.Abstractions.Loadouts.Files.Diff;
using NexusMods.App.UI.Controls.Trees.Common;
using NexusMods.App.UI.Resources;
using NexusMods.Sdk.Games;
using NexusMods.UI.Sdk;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Controls.Trees.Files;

[DebuggerDisplay("Key = {Key.ToString()}, ParentKey = {ParentKey.ToString()}")]
public class FileTreeNodeViewModel : AViewModel<IFileTreeNodeViewModel>, IFileTreeNodeViewModel
{
    [Reactive] public FileTreeNodeIconType Icon { get; set; }

    public bool IsFile { get; protected init; }
    
    public bool IsDeletion { get; protected init; } = false;
    public string Name { get; protected init; } = string.Empty;
    public ulong FileSize { get; set; }
    public uint FileCount { get; set; }
    public GamePath ParentKey { get; init; }
    public GamePath Key { get; set; }
    [Reactive] public bool IsExpanded { get; set; }
    public ReadOnlyObservableCollection<IFileTreeNodeViewModel>? Children { get; set; }
    public IFileTreeNodeViewModel? Parent { get; set; }
    
    public FileChangeType ChangeType { get; set; } = FileChangeType.None;

    protected FileTreeNodeViewModel()
    {
    }

    public FileTreeNodeViewModel(
        GamePath fullPath,
        GamePath parentPath,
        bool isFile,
        ulong fileSize,
        uint numChildFiles,
        bool isDeletion = false)
        : this(fullPath.FileName,
            fullPath,
            parentPath,
            isFile,
            fileSize,
            numChildFiles
        )
    {
        IsDeletion = isDeletion;
    }
    
    public FileTreeNodeViewModel(
        GamePath fullPath,
        GamePath parentPath,
        bool isFile,
        ulong fileSize,
        uint numChildFiles,
        FileChangeType changeType)
        : this(fullPath.FileName,
            fullPath,
            parentPath,
            isFile,
            fileSize,
            numChildFiles
        )
    {
        ChangeType = changeType;
    }

    public FileTreeNodeViewModel(
        string name,
        GamePath fullPath,
        GamePath parentPath,
        bool isFile,
        ulong fileSize,
        uint numChildFiles)
    {
        Name = name;
        Key = fullPath;
        ParentKey = parentPath;
        FileSize = fileSize;
        IsFile = isFile;
        Icon = isFile ? fullPath.Extension.GetIconType() : FileTreeNodeIconType.ClosedFolder;
        FileCount = numChildFiles;

        this.WhenActivated(d =>
            {
                this.WhenAnyValue(x => x.IsExpanded)
                    .Subscribe(isExpanded =>
                        {
                            if (IsFile) return;
                            Icon = isExpanded ? FileTreeNodeIconType.OpenFolder : FileTreeNodeIconType.ClosedFolder;
                        }
                    )
                    .DisposeWith(d);
            }
        );
    }
    
    internal static HierarchicalExpanderColumn<IFileTreeNodeViewModel> CreateTreeSourceNameColumn()
    {
        return new HierarchicalExpanderColumn<IFileTreeNodeViewModel>(
            new TemplateColumn<IFileTreeNodeViewModel>(
                Language.Helpers_GenerateHeader_NAME,
                "FileNameColumnTemplate",
                width: new GridLength(1, GridUnitType.Star),
                options: new TemplateColumnOptions<IFileTreeNodeViewModel>
                {
                    // Compares IsFile, to show folders first, then by file name.
                    CompareAscending = (x, y) =>
                    {
                        if (x == null || y == null) return 0;
                        var folderComparison = x.IsFile.CompareTo(y.IsFile);
                        return folderComparison != 0
                            ? folderComparison
                            : string.Compare(x.Name, y.Name, StringComparison.OrdinalIgnoreCase);
                    },

                    CompareDescending = (x, y) =>
                    {
                        if (x == null || y == null) return 0;
                        var folderComparison = x.IsFile.CompareTo(y.IsFile);
                        return folderComparison != 0
                            ? folderComparison
                            : string.Compare(y.Name, x.Name, StringComparison.OrdinalIgnoreCase);
                    },
                }
            ),
            node => node.Children,
            null,
            node => node.IsExpanded
        );
    }

    internal static TextColumn<IFileTreeNodeViewModel, string?> CreateTreeSourceFileCountColumn()
    {
        return new TextColumn<IFileTreeNodeViewModel, string?>(
            Language.Helpers_GenerateHeader_File_Count,
            x => x.ToFormattedFileCount(),
            options: new TextColumnOptions<IFileTreeNodeViewModel>
            {
                // Compares IsFile, to show folders first, then by file count for folders, and file name for files.
                CompareAscending = (x, y) =>
                {
                    if (x == null || y == null) return 0;
                    var folderComparison = x.IsFile.CompareTo(y.IsFile);
                    if (folderComparison != 0)
                        return folderComparison;

                    return x.IsFile
                        ? string.Compare(x.Name, y.Name, StringComparison.OrdinalIgnoreCase)
                        : x.FileCount.CompareTo(y.FileCount);
                },

                CompareDescending = (x, y) =>
                {
                    if (x == null || y == null) return 0;
                    var folderComparison = x.IsFile.CompareTo(y.IsFile);
                    if (folderComparison != 0)
                        return folderComparison;
                    // Always ascending for names, descending for file count.
                    return x.IsFile
                        ? string.Compare(x.Name, y.Name, StringComparison.OrdinalIgnoreCase)
                        : y.FileCount.CompareTo(x.FileCount);
                },
            },
            width: new GridLength(120)
        );
    }

    internal static TextColumn<IFileTreeNodeViewModel, string?> CreateTreeSourceSizeColumn()
    {
        return new TextColumn<IFileTreeNodeViewModel, string?>(
            Language.Helpers_GenerateHeader_SIZE,
            x => ByteSize.FromBytes(x.FileSize).ToString(),
            options: new TextColumnOptions<IFileTreeNodeViewModel>
            {
                // Compares if folder first, such that folders show first, then by file size.
                CompareAscending = (x, y) =>
                {
                    if (x == null || y == null) return 0;
                    var folderComparison = x.IsFile.CompareTo(y.IsFile);
                    return folderComparison != 0 ? folderComparison : x.FileSize.CompareTo(y.FileSize);
                },

                CompareDescending = (x, y) =>
                {
                    if (x == null || y == null) return 0;
                    var folderComparison = x.IsFile.CompareTo(y.IsFile);
                    return folderComparison != 0 ? folderComparison : y.FileSize.CompareTo(x.FileSize);
                },
            },
            width: new GridLength(100)
        );
    }
    
    internal static TemplateColumn<IFileTreeNodeViewModel> CreateTreeSourceStateColumn()
    {
        return new TemplateColumn<IFileTreeNodeViewModel>(
            Language.Helpers_GenerateHeader_State,
            "FileStateColumnTemplate",
            width: new GridLength(150),
            options: new TemplateColumnOptions<IFileTreeNodeViewModel>
            {
                // Compares change state first, then by folder/file, then by name.
                CompareAscending = (x, y) =>
                {
                    if (x == null || y == null) return 0;
                    var folderComparison = x.IsFile.CompareTo(y.IsFile);
                    // inverted comparison due to the enum values
                    var changeComparison = y.ChangeType.CompareTo(x.ChangeType);
                    
                    if (changeComparison!= 0) return changeComparison;
                    return folderComparison != 0 
                        ? folderComparison 
                        : string.Compare(x.Name, y.Name, StringComparison.OrdinalIgnoreCase);
                },

                CompareDescending = (x, y) =>
                {
                    if (x == null || y == null) return 0;
                    var folderComparison = x.IsFile.CompareTo(y.IsFile);
                    var changeComparison = x.ChangeType.CompareTo(y.ChangeType);
                    
                    if (changeComparison!= 0) return changeComparison;
                    return folderComparison != 0 
                        ? folderComparison 
                        : string.Compare(y.Name, x.Name, StringComparison.OrdinalIgnoreCase);
                },
            }
        );
    }
}
