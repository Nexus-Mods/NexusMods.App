using Microsoft.Extensions.DependencyInjection;
using NexusMods.DataModel;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.Loadouts.Markers;
using NexusMods.DataModel.ModInstallers;
using NexusMods.Paths;

namespace NexusMods.Games.TestFramework;

public abstract class AModInstallerTest<TGame, TModInstaller> : AGameTest<TGame>
    where TGame : AGame
    where TModInstaller : IModInstaller
{
    protected readonly TModInstaller ModInstaller;

    protected readonly FileContentsCache FileContentsCache;

    /// <summary>
    /// Constructor.
    /// </summary>
    protected AModInstallerTest(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        ModInstaller = serviceProvider.FindImplementationInContainer<TModInstaller, IModInstaller>();

        FileContentsCache = serviceProvider.GetRequiredService<FileContentsCache>();
    }

    protected async Task InstallModWithInstaller(AbsolutePath path)
    {
        // LoadoutManager.ArchiveManager.ArchiveFileAsync()

        var analyzedFile = await FileContentsCache.AnalyzeFileAsync(path);
        ModInstaller.GetFilesToExtractAsync(GameInstallation, )
    }
}
