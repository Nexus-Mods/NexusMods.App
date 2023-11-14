using System.Collections.ObjectModel;
using System.Reactive;
using NexusMods.Games.AdvancedInstaller.UI.ModContent;
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
    public bool IsNew { get; private set; }
    public bool IsFolderMerged { get; set; }
    public bool IsFolderDupe { get; }

    public IModContentTreeEntryViewModel? MappedEntry { get; set; }

    public ObservableCollection<IModContentTreeEntryViewModel> MappedEntries { get; } = new();
    public ReactiveCommand<Unit, Unit> RemoveMappingCommand { get; }

    public PreviewTreeEntryViewModel(
        GamePath gamePath,
        bool isDirectory,
        bool isNew)
    {
        GamePath = gamePath;
        IsDirectory = isDirectory;
        IsNew = isNew;

        IsRoot = GamePath.Path == RelativePath.Empty;
        Parent = IsRoot ? IPreviewTreeEntryViewModel.RootParentGamePath : GamePath.Parent;
        DisplayName = gamePath.FileName == RelativePath.Empty ? gamePath.LocationId.Value : gamePath.FileName;
        IsFolderDupe = gamePath.FileName == gamePath.Parent.FileName && gamePath.FileName != RelativePath.Empty;
        IsFolderMerged = false;
        IsRemovable = false;

        RemoveMappingCommand = ReactiveCommand.Create(() => { });
    }

    public void AddMapping(IModContentTreeEntryViewModel entry)
    {
        IsNew = true;
        IsRemovable = true;
        if (IsDirectory)
        {
            MappedEntries.Add(entry);
        }
        else
        {
            MappedEntry = entry;
        }
    }

    public void RemoveFileMapping()
    {
        MappedEntry = null;
        IsNew = false;
        IsRemovable = false;
    }

    public void RemoveDirectoryMapping(IModContentTreeEntryViewModel entry)
    {
        MappedEntries.Remove(entry);
        if (MappedEntries.Count != 0) return;
        IsNew = false;
        IsRemovable = false;
    }
}
