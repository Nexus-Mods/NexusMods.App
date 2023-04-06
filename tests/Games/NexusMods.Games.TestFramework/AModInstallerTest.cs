using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.DataModel;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Abstractions.Ids;
using NexusMods.DataModel.ArchiveContents;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.Markers;
using NexusMods.DataModel.ModInstallers;
using NexusMods.Paths;

namespace NexusMods.Games.TestFramework;

[PublicAPI]
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

    /// <summary>
    /// Runs the <typeparamref name="TModInstaller"/> and returns a list of files
    /// to extract from the provided archive.
    /// </summary>
    /// <param name="path">Path to the archive to extract.</param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException">The file at the provided path is not an archive.</exception>
    protected async Task<AModFile[]> GetFilesToExtractFromInstaller(AbsolutePath path)
    {
        var analyzedFile = await FileContentsCache.AnalyzeFileAsync(path);
        if (analyzedFile is not AnalyzedArchive analyzedArchive)
        {
            // see LoadoutManager.InstallModAsync
            throw new NotImplementedException();
        }

        var contents = await ModInstaller.GetFilesToExtractAsync(
            GameInstallation,
            analyzedFile.Hash,
            analyzedArchive.Contents);

        return contents.ToArray();
    }

    /// <summary>
    /// Installs the archive at the provided path with the <typeparamref name="TModInstaller"/>
    /// and returns the <see cref="Mod"/> of it.
    /// </summary>
    /// <param name="loadout"></param>
    /// <param name="path"></param>
    /// <returns></returns>
    protected async Task<Mod> InstallModWithInstaller(LoadoutMarker loadout, AbsolutePath path)
    {
        var contents = await GetFilesToExtractFromInstaller(path);

        var newMod = new Mod
        {
            Id = ModId.New(),
            Name = path.FileName,
            Files = new EntityDictionary<ModFileId, AModFile>(DataStore, contents.Select(c => new KeyValuePair<ModFileId, IId>(c.Id, c.DataStoreId)))
        };

        loadout.Add(newMod);
        return newMod;
    }
}
