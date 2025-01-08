using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.UI;
using NexusMods.Paths;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.Games.AdvancedInstaller.UI.SelectLocation;

public class SelectableTreeEntryViewModel : AViewModel<ISelectableTreeEntryViewModel>,
    ISelectableTreeEntryViewModel
{
    public GamePath GamePath { get; }
    public string DisplayName { get; }
    [Reactive] public string InputText { get; set; }
    [Reactive] private bool CanSave { get; set; }
    [Reactive] public bool IsExpanded { get; set; }
    public bool IsRoot { get; }
    public GamePath Parent { get; }
    [Reactive] public SelectableDirectoryNodeStatus Status { get; set; }
    public ReactiveCommand<Unit, Unit> CreateMappingCommand { get; }
    public ReactiveCommand<Unit, Unit> EditCreateFolderCommand { get; }
    public ReactiveCommand<Unit, Unit> SaveCreatedFolderCommand { get; }
    public ReactiveCommand<Unit, Unit> CancelCreateFolderCommand { get; }
    public ReactiveCommand<Unit, Unit> DeleteCreatedFolderCommand { get; }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="gamePath">GamePath must be a directory and be relative to a TopLevelLocation</param>
    /// <param name="status"></param>
    public SelectableTreeEntryViewModel(
        GamePath gamePath,
        SelectableDirectoryNodeStatus status)
    {
        GamePath = gamePath;
        Status = status;

        IsRoot = GamePath.Path == RelativePath.Empty;
        // Use invalid parent path for root node, to avoid matching another node by accident.
        Parent = IsRoot ? ISelectableTreeEntryViewModel.RootParentGamePath : GamePath.Parent;
        DisplayName = gamePath.FileName == RelativePath.Empty ? gamePath.LocationId.ToString() : gamePath.FileName;
        InputText = string.Empty;

        CreateMappingCommand = ReactiveCommand.Create(() => { });
        EditCreateFolderCommand = ReactiveCommand.Create(() => { });
        SaveCreatedFolderCommand = ReactiveCommand.Create(() => { },
            this.WhenAnyValue(x => x.CanSave));
        CancelCreateFolderCommand = ReactiveCommand.Create(() => { });
        DeleteCreatedFolderCommand = ReactiveCommand.Create(() => { });

        this.WhenActivated(disposables =>
        {
            this.WhenAnyValue(x => x.InputText)
                .Select(text => text != string.Empty && RemoveInvalidFolderCharacter(text) != string.Empty)
                .BindToVM(this, vm => vm.CanSave)
                .DisposeWith(disposables);
        });
    }

    public RelativePath GetSanitizedInput()
    {
        return RelativePath.FromUnsanitizedInput(RemoveInvalidFolderCharacter(InputText));
    }

    /// <summary>
    /// Pattern to match invalid characters in folder names.
    /// </summary>
    private static readonly string InvalidFolderCharsPattern =
        "[" + String.Concat(Path.GetInvalidFileNameChars().Concat(new[] { '\\', '/' })) + "]";

    /// <summary>
    /// Compiled Regex to match invalid characters in folder names.
    /// </summary>
    private static readonly Regex InvalidFolderCharsRegex = new(InvalidFolderCharsPattern, RegexOptions.Compiled);


    private static string RemoveInvalidFolderCharacter(string name)
    {
        var trimmed = name.Trim();
        return trimmed == string.Empty ? trimmed : InvalidFolderCharsRegex.Replace(trimmed, "").Trim();
    }
}
