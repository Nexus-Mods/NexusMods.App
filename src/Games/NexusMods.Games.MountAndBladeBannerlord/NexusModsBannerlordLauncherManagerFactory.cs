using System.Collections.Concurrent;
using Bannerlord.LauncherManager;
using NexusMods.DataModel.Games;
using NexusMods.Paths;

namespace NexusMods.Games.MountAndBladeBannerlord;

public class NexusModsBannerlordLauncherManagerFactory
{
    private readonly ConcurrentDictionary<GameInstallation, LauncherManagerHandler> _instances = new();

    // TODO: We need a better key
    public LauncherManagerHandler Get(GameInstallation installation) => _instances.GetOrAdd(installation, static x =>
    {
        var handler = new LauncherManagerHandler();
        handler.RegisterCallbacks(
            loadLoadOrder: null!, // TODO:
            saveLoadOrder: null!, // TODO:
            sendNotification: null!, // TODO:
            sendDialog: null!, // TODO:
            setGameParameters: null!, // TODO:
            getInstallPath: () => x.Locations.First(x => x.Key == GameFolderType.Game).Value.ToString(),
            readFileContent: (path, offset, length) =>
            {
                if (!File.Exists(path)) return null;

                if (offset == 0 && length == -1)
                {
                    return File.ReadAllBytes(path);
                }
                else if (offset >= 0 && length > 0)
                {
                    using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                    var data = new byte[length];
                    fs.Seek(offset, SeekOrigin.Begin);
                    fs.Read(data, 0, length);
                    return data;
                }
                else
                {
                    return null;
                }
            },
            writeFileContent: File.WriteAllBytes,
            readDirectoryFileList: Directory.GetFiles,
            readDirectoryList: Directory.GetDirectories,
            getModuleViewModels: null!, // TODO:
            setModuleViewModels: null!, // TODO:
            getOptions: null!, // TODO:
            getState: null! // TODO:
        );
        return handler;
    });
}