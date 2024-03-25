using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reactive.Disposables;
using NexusMods.Abstractions.GameLocators;
using NexusMods.App.UI.Controls.Trees.Common;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Controls.Trees.Files;

[DebuggerDisplay("Key = {Key.ToString()}, ParentKey = {ParentKey.ToString()}")]
public class FileTreeNodeViewModel : AViewModel<IFileTreeNodeViewModel>, IFileTreeNodeViewModel
{
    [Reactive]
    public FileTreeNodeIconType Icon { get; set; }

    public bool IsFile { get; protected init; }
    public string Name => Key.FileName;
    public ulong FileSize { get; protected init; }
    public GamePath ParentKey { get; init; }
    public GamePath Key { get; set; }
    [Reactive] public bool IsExpanded { get; set; }
    public ReadOnlyObservableCollection<IFileTreeNodeViewModel>? Children { get; set; }
    public IFileTreeNodeViewModel? Parent { get; set; }

    protected FileTreeNodeViewModel() { }
    
    public FileTreeNodeViewModel(GamePath fullPath, GamePath parentPath, bool isFile, ulong fileSize)
    {
        Key = fullPath;
        ParentKey = parentPath;
        FileSize = fileSize;
        IsFile = isFile;
        Icon = isFile ? fullPath.Extension.GetIconType() : FileTreeNodeIconType.ClosedFolder;
        
        this.WhenActivated(d =>
        {
            this.WhenAnyValue(x => x.IsExpanded)
                .Subscribe(isExpanded =>
                {
                    if (IsFile) return;
                    Icon = isExpanded ? FileTreeNodeIconType.OpenFolder : FileTreeNodeIconType.ClosedFolder;
                })
                .DisposeWith(d);
        });
    }
}
