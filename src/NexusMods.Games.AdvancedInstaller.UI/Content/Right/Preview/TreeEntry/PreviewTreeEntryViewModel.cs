using System.Collections.ObjectModel;
using System.Reactive;
using DynamicData.Kernel;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.UI;
using NexusMods.Games.AdvancedInstaller.UI.ModContent;
using NexusMods.Paths;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

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
    [Reactive] public bool IsExpanded { get; set; }

    public Optional<IModContentTreeEntryViewModel> MappedEntry { get; set; }

    public ObservableCollection<IModContentTreeEntryViewModel> MappedEntries { get; } = new();
    public ReactiveCommand<Unit, Unit> RemoveMappingCommand { get; }

    /// <summary>
    /// Constructs a Preview tree entry view model.
    /// </summary>
    /// <param name="gamePath">The GamePath relative to a top level LocationId uniquely identifying this entry.</param>
    /// <param name="isDirectory">Whether the entry represents a directory.</param>
    /// <param name="isNew">Whether the entry should be marked as new.</param>
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
        DisplayName = gamePath.FileName == RelativePath.Empty ? gamePath.LocationId.ToString() : gamePath.FileName;
        IsFolderDupe = gamePath.FileName == gamePath.Parent.FileName && gamePath.FileName != RelativePath.Empty;
        IsFolderMerged = false;
        IsRemovable = true;

        RemoveMappingCommand = ReactiveCommand.Create(() => { });
    }

    public void AddMapping(IModContentTreeEntryViewModel entry)
    {
        IsNew = true;
        if (IsDirectory)
        {
            MappedEntries.Add(entry);
        }
        else
        {
            MappedEntry = Optional<IModContentTreeEntryViewModel>.Create(entry);
        }
    }

    public void RemoveFileMapping()
    {
        MappedEntry = null;
        IsNew = false;
    }

    public void RemoveDirectoryMapping(IModContentTreeEntryViewModel entry)
    {
        MappedEntries.Remove(entry);
        if (MappedEntries.Count != 0) return;
        IsNew = false;
    }
}
