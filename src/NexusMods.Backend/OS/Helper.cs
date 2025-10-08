using Microsoft.Extensions.Logging;
using NexusMods.Paths;

namespace NexusMods.Backend.OS;

internal static class Helper
{
    public static bool AssertIsFile(AbsolutePath filePath, ILogger logger)
    {
        if (filePath.FileExists) return true;
        if (filePath.DirectoryExists()) logger.LogError("Unable to open file at `{Path}` because it isn't a file", filePath);
        else logger.LogError("Unable to open file at `{Path}` because it doesn't exist", filePath);
        return false;
    }

    public static bool AssertIsDirectory(AbsolutePath directoryPath, ILogger logger)
    {
        if (directoryPath.DirectoryExists()) return true;
        if (directoryPath.FileExists) logger.LogError("Unable to open directory at `{Path}` because it isn't a directory", directoryPath);
        else logger.LogError("Unable to open directory at `{Path}` because it doesn't exist", directoryPath);
        return false;
    }
}
