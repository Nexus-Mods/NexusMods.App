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

    private byte[] ReadFileContent(AbsolutePath filePath, int offset, int length)
    {
        using var fs = _fileSystem.ReadFile(filePath);
        fs.Seek(offset, SeekOrigin.Begin);

        var toRead = length == -1 ? (int) fs.Length : length;
        var data = GC.AllocateUninitializedArray<byte>(toRead);
        
        fs.ReadAtLeast(data, toRead, false);
        return data;
    }

    public byte[]? ReadFileContent(string unsanitizedFilePath, int offset, int length)
    {
        var filePath = _fileSystem.FromUnsanitizedFullPath(unsanitizedFilePath);
        if (!_fileSystem.FileExists(filePath)) return null;

        try
        {
            if (offset == 0 && length == -1)
            {
                return ReadFileContent(filePath, offset, length);
            }
            
            if (offset >= 0 && length > 0)
            {
                var data = GC.AllocateUninitializedArray<byte>(length);
                _fileSystem.ReadBytesRandomAccess(filePath, data, offset);
                return data;
            }
            
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bannerlord IO Read Operation failed! {Path}", unsanitizedFilePath);
            return null;
        }
    }

    public void WriteFileContent(string unsanitizedFilePath, byte[]? data)
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
                using var fs = _fileSystem.WriteFile(filePath);
                fs.Write(data, 0, data.Length);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bannerlord IO Write Operation failed! {Path}", unsanitizedFilePath);
        }
    }

    public string[]? ReadDirectoryFileList(string unsanitizedDirectoryPath)
    {
        var directoryPath = _fileSystem.FromUnsanitizedFullPath(unsanitizedDirectoryPath);

        return _fileSystem.DirectoryExists(directoryPath)
            ? _fileSystem.EnumerateFiles(directoryPath).Select(x => x.GetFullPath()).ToArray()
            : null;
    }

    public string[]? ReadDirectoryList(string unsanitizedDirectoryPath)
    {
        var directoryPath = _fileSystem.FromUnsanitizedFullPath(unsanitizedDirectoryPath);
        
        return _fileSystem.DirectoryExists(directoryPath)
            ? _fileSystem.EnumerateDirectories(directoryPath).Select(x => x.GetFullPath()).ToArray()
            : null;
    }
}
