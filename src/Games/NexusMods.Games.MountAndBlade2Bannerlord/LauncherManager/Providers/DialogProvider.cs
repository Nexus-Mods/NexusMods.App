using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Bannerlord.LauncherManager.External.UI;
using Bannerlord.LauncherManager.Models;

namespace NexusMods.Games.MountAndBlade2Bannerlord.LauncherManager.Providers;

internal sealed class DialogProvider : IDialogProvider
{
    private Window? _window; // TODO: How to inject the window?

    public void SendDialog(DialogType type, string title, string message, IReadOnlyList<DialogFileFilter> filters, Action<string> onResult)
    {
        if (_window is null)
        {
            onResult(string.Empty);
            return;
        }

        switch (type)
        {
            case DialogType.Warning:
            {
                var split = message.Split(new[] { "--CONTENT-SPLIT--" }, StringSplitOptions.RemoveEmptyEntries);
                /*
                    using var okButton = new TaskDialogButton(ButtonType.Yes);
                    using var cancelButton = new TaskDialogButton(ButtonType.No);
                    using var dialog = new TaskDialog
                    {
                        MainIcon = TaskDialogIcon.Warning,
                        WindowTitle = new BUTRTextObject(title).ToString(),
                        MainInstruction = split[0],
                        Content = split.Length > 1 ? split[1] : string.Empty,
                        Buttons = { okButton, cancelButton },
                        CenterParent = true,
                        AllowDialogCancellation = true,
                    };
                    onResult(dialog.ShowDialog() == okButton ? "true" : "false");
                    */
                return;
            }
            case DialogType.FileOpen:
            {
                // No idea if correct
                _window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                {
                    Title = title,
                    FileTypeFilter = filters.Select(x => new FilePickerFileType(x.Name) { Patterns = x.Extensions }).ToArray(),
                    AllowMultiple = false
                }).ContinueWith(x => onResult(x.Result.Count < 1 ? string.Empty : x.Result[0].Path.ToString()));
                return;
            }
            case DialogType.FileSave:
            {
                var fileName = message;

                // No idea if correct
                _window.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
                {
                    Title = title,
                    FileTypeChoices = filters.Select(x => new FilePickerFileType(x.Name) { Patterns = x.Extensions }).ToArray(),
                    SuggestedFileName = fileName,
                    ShowOverwritePrompt = true
                }).ContinueWith(x => onResult(x.Result is null ? string.Empty : x.Result.Path.ToString()));
                return;
            }
        }
    }
}
