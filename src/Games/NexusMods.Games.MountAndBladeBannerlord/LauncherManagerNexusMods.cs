using Bannerlord.LauncherManager;
using Microsoft.Extensions.Logging;

namespace NexusMods.Games.MountAndBladeBannerlord;

internal sealed class LauncherManagerNexusMods : LauncherManagerHandler
{
    private readonly ILogger _logger;
    private readonly string _installationPath;

    public string ExecutableParameters { get; private set; } = string.Empty;
    
    public LauncherManagerNexusMods(ILogger<LauncherManagerNexusMods> logger, string installationPath)
    {
        _logger = logger;
        _installationPath = installationPath;
        RegisterCallbacks(
            loadLoadOrder: null!, // TODO:
            saveLoadOrder: null!, // TODO:
            sendNotification: null!, // TODO:
            sendDialog: null!, // TODO:
            setGameParameters: (executable, parameters) =>
            {
                ExecutableParameters = string.Join(" ", parameters);
            },
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