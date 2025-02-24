using Avalonia.Platform.Storage;
using JetBrains.Annotations;
using NexusMods.App.UI.Extensions;
using NexusMods.Paths;

namespace NexusMods.App.UI;

[PublicAPI]
public interface IAvaloniaInterop
{
    void RegisterStorageProvider(IStorageProvider storageProvider);

    Task<AbsolutePath[]> OpenFilePickerAsync(FilePickerOpenOptions filePickerOpenOptions);
}

internal class AvaloniaInterop : IAvaloniaInterop
{
    private IStorageProvider? _storageProvider;

    public void RegisterStorageProvider(IStorageProvider storageProvider)
    {
        _storageProvider = storageProvider;
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
