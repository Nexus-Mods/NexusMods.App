using System.Reactive;
using System.Reactive.Disposables;
using System.Text.RegularExpressions;
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
    public bool IsRoot { get; }

    public GamePath Parent { get; }
    [Reactive] public SelectableDirectoryNodeStatus Status { get; set; }
    public bool HasActiveLink { get; set; }
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
        Parent = IsRoot ? new GamePath(LocationId.Unknown, "") : GamePath.Parent;
        DisplayName = gamePath.FileName == RelativePath.Empty ? gamePath.LocationId.Value : gamePath.FileName;
        InputText = string.Empty;
        HasActiveLink = false;

        CreateMappingCommand = ReactiveCommand.Create(() => { });
        EditCreateFolderCommand = ReactiveCommand.Create(() => { });
        SaveCreatedFolderCommand = ReactiveCommand.Create(() => { }, this.WhenAnyValue(x => x.CanSave));
        CancelCreateFolderCommand = ReactiveCommand.Create(() => { });
        DeleteCreatedFolderCommand = ReactiveCommand.Create(() => { });

        this.WhenActivated(disposables =>
        {
            this.WhenAnyValue(x => x.InputText)
                .Subscribe(text =>
                {
                    if (text == string.Empty)
                    {
                        CanSave = false;
                    }
                    else
                    {
                        var trimmed = RemoveInvalidFolderCharacter(text);
                        CanSave = trimmed != string.Empty;
                    }
                })
                .DisposeWith(disposables);
        });
    }

    public RelativePath GetSanitizedInput()
    {
        return RelativePath.FromUnsanitizedInput(RemoveInvalidFolderCharacter(InputText));
    }

    private static readonly string InvalidFolderCharsRegex =
        "[" + String.Concat(System.IO.Path.GetInvalidFileNameChars().Concat(new[] { '\\', '/' })) + "]";

    private string RemoveInvalidFolderCharacter(string name)
    {
        var trimmed = name.Trim();
        if (trimmed == string.Empty)
            return trimmed;
        trimmed = Regex.Replace(trimmed, InvalidFolderCharsRegex, "");
        return trimmed;
    }
}
