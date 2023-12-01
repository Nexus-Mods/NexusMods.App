using Bannerlord.LauncherManager.External;
using Microsoft.Extensions.Logging;

namespace NexusMods.Games.MountAndBlade2Bannerlord.LauncherManager.Providers;

// Stateless, can be singleton
internal sealed class FileSystemProvider : IFileSystemProvider
{
    private readonly ILogger _logger;
    public FileSystemProvider(ILogger<FileSystemProvider> logger)
    {
        _logger = logger;
    }

    public byte[]? ReadFileContent(string filePath, int offset, int length)
    {
        if (!File.Exists(filePath)) return null;

        try
        {
            if (offset == 0 && length == -1)
            {
                return File.ReadAllBytes(filePath);
            }
            else if (offset >= 0 && length > 0)
            {
                var data = new byte[length];
                using var handle = File.OpenHandle(filePath, options: FileOptions.RandomAccess);
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
            _logger.LogError(ex, "Bannerlord IO Read Operation failed! {Path}", filePath);
            return null;
        }
    }

    public void WriteFileContent(string filePath, byte[]? data)
    {
        if (!File.Exists(filePath)) return;

        try
        {
            if (data is null)
                File.Delete(filePath);
            else
                File.WriteAllBytes(filePath, data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bannerlord IO Write Operation failed! {Path}", filePath);
        }
    }

    public string[]? ReadDirectoryFileList(string directoryPath) => Directory.Exists(directoryPath) ? Directory.GetFiles(directoryPath) : null;

    public string[]? ReadDirectoryList(string directoryPath) => Directory.Exists(directoryPath) ? Directory.GetFiles(directoryPath) : null;
}
