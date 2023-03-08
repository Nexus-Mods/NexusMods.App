using System.Collections.Concurrent;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Bannerlord.LauncherManager;
using Bannerlord.LauncherManager.Localization;
using Bannerlord.LauncherManager.Models;
using Microsoft.Extensions.Logging;

namespace NexusMods.Games.MountAndBlade2Bannerlord;

internal sealed partial class LauncherManagerNexusMods : LauncherManagerHandler
{
    private readonly ILogger _logger;
    private readonly string _installationPath;
    private readonly ConcurrentDictionary<string, object?> _notificationIds = new();

    private Window? _window; // TODO: How to inject the window?

    public string ExecutableParameters { get; private set; } = string.Empty;

    public LauncherManagerNexusMods(ILogger<LauncherManagerNexusMods> logger, string installationPath)
    {
        _logger = logger;
        _installationPath = installationPath;

        RegisterCallbacks(
            loadLoadOrder: null!, // TODO:
            saveLoadOrder: null!, // TODO:
            sendNotification:  SendNotificationDelegate,
            sendDialog: SendDialogDelegate,
            setGameParameters: (executable, parameters) => ExecutableParameters = string.Join(" ", parameters),
            getInstallPath: () => installationPath,
            readFileContent: ReadFileContentDelegate,
            writeFileContent: WriteFileContentDelegate,
            readDirectoryFileList: Directory.GetFiles,
            readDirectoryList: Directory.GetDirectories,
            getModuleViewModels: null!, // TODO:
            setModuleViewModels: null!, // TODO:
            getOptions: null!, // TODO:
            getState: null! // TODO:
        );
    }

    public void SetCurrentWindow(Window window)
    {
        _window = window;
    }

    private void SendNotificationDelegate(string id, NotificationType type, string message, uint ms)
    {
        if (_window is null)
        {
            return;
        }

        if (string.IsNullOrEmpty(id)) id = Guid.NewGuid().ToString();

        // Prevents message spam
        if (_notificationIds.TryAdd(id, null)) return;
        using var cts = new CancellationTokenSource();
        _ = Task.Delay(TimeSpan.FromMilliseconds(ms), cts.Token).ContinueWith(x => _notificationIds.TryRemove(id, out _), cts.Token);

        var translatedMessage = new BUTRTextObject(message).ToString();
        switch (type)
        {
            case NotificationType.Hint:
            {
                //HintManager.ShowHint(translatedMessage);
                cts.Cancel();
                break;
            }
            case NotificationType.Info:
            {
                // TODO:
                //HintManager.ShowHint(translatedMessage);
                cts.Cancel();
                break;
            }
            default:
                //MessageBox.Show(translatedMessage);
                cts.Cancel();
                break;
        }
    }

    private void SendDialogDelegate(DialogType type, string title, string message, IReadOnlyList<DialogFileFilter> filters, Action<string> onResult)
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


    private byte[]? ReadFileContentDelegate(string path, int offset, int length)
    {
        if (!File.Exists(path)) return null;

        try
        {
            if (offset == 0 && length == -1)
            {
                return File.ReadAllBytes(path);
            }
            else if (offset >= 0 && length > 0)
            {
                var data = new byte[length];
                using var handle = File.OpenHandle(path, options: FileOptions.RandomAccess);
                RandomAccess.Read(handle, data, offset);
                return data;
            }
            else
            {
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bannerlord IO Read Operation failed! {Path}", path);
            return null;
        }
    }

    private void WriteFileContentDelegate(string path, byte[]? data)
    {
        if (!File.Exists(path)) return;

        try
        {
            if (data is null)
                File.Delete(path);
            else
                File.WriteAllBytes(path, data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bannerlord IO Write Operation failed! {Path}", path);
        }
    }
}
