using Bannerlord.LauncherManager.External;
using Microsoft.Extensions.Logging;
using NexusMods.Paths;

namespace NexusMods.Games.MountAndBlade2Bannerlord.LauncherManager;

internal sealed class FileSystemProvider : IFileSystemProvider
{
    private readonly ILogger _logger;
    private readonly IFileSystem _fileSystem;

    public FileSystemProvider(ILogger<FileSystemProvider> logger, IFileSystem fileSystem)
    {
        _logger = logger;
        _fileSystem = fileSystem;
    }

    public async Task<byte[]?> ReadFileContentAsync(string unsanitizedFilePath, int offset, int length)
    {
        var filePath = _fileSystem.FromUnsanitizedFullPath(unsanitizedFilePath);
        if (!_fileSystem.FileExists(filePath)) return null;

        try
        {
            if (length == -1)
                length = (int) _fileSystem.GetFileEntry(filePath).Size.Value;
            
            if (length == 0)
                return [];
            
            var data = GC.AllocateUninitializedArray<byte>(length);
            await _fileSystem.ReadBytesRandomAccessAsync(filePath, data, offset);
            return data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bannerlord IO Read Operation failed! {Path}", unsanitizedFilePath);
            return null;
        }
    }

    public async Task WriteFileContentAsync(string unsanitizedFilePath, byte[]? data)
    {
        var filePath = _fileSystem.FromUnsanitizedFullPath(unsanitizedFilePath);
        if (!_fileSystem.FileExists(filePath)) return;

        try
        {
            if (data is null)
            {
                _fileSystem.DeleteFile(filePath);
            }
            else
            {
                await using var fs = _fileSystem.WriteFile(filePath);
                await fs.WriteAsync(data);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bannerlord IO Write Operation failed! {Path}", unsanitizedFilePath);
        }
    }

    public Task<string[]?> ReadDirectoryFileListAsync(string unsanitizedDirectoryPath)
    {
        var directoryPath = _fileSystem.FromUnsanitizedFullPath(unsanitizedDirectoryPath);

        return Task.FromResult(_fileSystem.DirectoryExists(directoryPath)
            ? _fileSystem.EnumerateFiles(directoryPath).Select(x => x.GetFullPath()).ToArray()
            : null);
    }

    public Task<string[]?> ReadDirectoryListAsync(string unsanitizedDirectoryPath)
    {
        var directoryPath = _fileSystem.FromUnsanitizedFullPath(unsanitizedDirectoryPath);
        
        return Task.FromResult(_fileSystem.DirectoryExists(directoryPath)
            ? _fileSystem.EnumerateDirectories(directoryPath).Select(x => x.GetFullPath()).ToArray()
            : null);
    }
}
