using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Common;
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

    /// <summary>
    /// Constructor.
    /// </summary>
    protected AModInstallerTest(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        ModInstaller = serviceProvider.FindImplementationInContainer<TModInstaller, IModInstaller>();
    }

    /// <summary>
    /// Runs the <typeparamref name="TModInstaller"/> and returns the priority
    /// for the given archive.
    /// </summary>
    /// <param name="path">Path to the archive to extract.</param>
    /// <returns></returns>
    protected async Task<Priority> GetPriorityFromInstaller(AbsolutePath path)
    {
        var analyzedArchive = await AnalyzeArchive(path);

        var priority = ModInstaller.Priority(
            GameInstallation,
            analyzedArchive.Contents);

        return priority;
    }

    /// <summary>
    /// Runs the <typeparamref name="TModInstaller"/> and returns a list of files
    /// to extract from the provided archive.
    /// </summary>
    /// <param name="path">Path to the archive to extract.</param>
    /// <returns></returns>
    protected async Task<AModFile[]> GetFilesToExtractFromInstaller(AbsolutePath path)
    {
        var analyzedArchive = await AnalyzeArchive(path);

        var contents = await ModInstaller.GetFilesToExtractAsync(
            GameInstallation,
            analyzedArchive.Hash,
            analyzedArchive.Contents);

        return contents.WithPersist(DataStore).ToArray();
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
