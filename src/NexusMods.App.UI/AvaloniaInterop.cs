using Avalonia.Input.Platform;
using Avalonia.Platform.Storage;
using JetBrains.Annotations;
using NexusMods.App.UI.Extensions;
using NexusMods.Paths;

namespace NexusMods.App.UI;

[PublicAPI]
public interface IAvaloniaInterop
{
    void RegisterStorageProvider(IStorageProvider storageProvider);
    void RegisterClipboard(IClipboard clipboardProvider);
    
    Task<AbsolutePath[]> OpenFilePickerAsync(FilePickerOpenOptions filePickerOpenOptions);
    Task SetClipboardTextAsync(string text);
}



internal class AvaloniaInterop : IAvaloniaInterop
{
    private IStorageProvider? _storageProvider;
    private IClipboard? _clipboard;

    public void RegisterStorageProvider(IStorageProvider storageProvider)
    {
        _storageProvider = storageProvider;
    }

    public void RegisterClipboard(IClipboard clipboard)
    {
        _clipboard = clipboard;
    }
    
    // a function to set the clipboard text
    
    public async Task SetClipboardTextAsync(string text)
    {
        var clipboard = _clipboard;
        if (clipboard is null) throw new InvalidOperationException("No clipboard registered!");

        try
        {
            await clipboard.SetTextAsync(text);
        }
        catch (Exception ex)
        {
            // Log the exception or handle it as needed
            throw new InvalidOperationException("Failed to set clipboard text.", ex);
        }
    }

    public async Task<AbsolutePath[]> OpenFilePickerAsync(FilePickerOpenOptions filePickerOpenOptions)
    {
        var storageProvider = _storageProvider;
        if (storageProvider is null) throw new InvalidOperationException("No storage provider registered!");

        try
        {
            var files = await storageProvider.OpenFilePickerAsync(filePickerOpenOptions);

            var paths = files
                .Select(file => file.TryGetLocalPath())
                .NotNull()
                .Select(path => FileSystem.Shared.FromUnsanitizedFullPath(path))
                .Where(path => path.FileExists)
                .ToArray();

            return paths;
        }
        catch (Exception)
        {
            return [];
        }
    }
}
