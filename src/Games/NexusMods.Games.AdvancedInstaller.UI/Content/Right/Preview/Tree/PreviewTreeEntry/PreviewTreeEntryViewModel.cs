using System.Reactive;
using NexusMods.Paths;
using ReactiveUI;

namespace NexusMods.Games.AdvancedInstaller.UI.Preview;

public class PreviewTreeEntryViewModel : AViewModel<IPreviewTreeEntryViewModel>, IPreviewTreeEntryViewModel
{
    public GamePath GamePath { get; }
    public GamePath Parent { get; }
    public string DisplayName { get; }

    public bool IsDirectory { get; }
    public bool IsRoot { get; }
    public bool IsRemovable { get; set; }
    public bool IsNew { get; }
    public bool IsFolderMerged { get; set; }
    public bool IsFolderDupe { get; set; }
    public ReactiveCommand<Unit, Unit> RemoveMappingCommand { get; }

    public PreviewTreeEntryViewModel(
        GamePath gamePath,
        bool isDirectory,
        bool isNew,
        bool isFolderDupe)
    {
        GamePath = gamePath;
        IsDirectory = isDirectory;
        IsNew = isNew;
        IsFolderDupe = isFolderDupe;

        IsRoot = GamePath.Path == RelativePath.Empty;
        Parent = IsRoot ? IPreviewTreeEntryViewModel.RootParentGamePath : GamePath.Parent;
        DisplayName = gamePath.FileName == RelativePath.Empty ? gamePath.LocationId.Value : gamePath.FileName;
        IsFolderMerged = false;
        IsRemovable = false;

        RemoveMappingCommand = ReactiveCommand.Create(() => { });
    }
}
