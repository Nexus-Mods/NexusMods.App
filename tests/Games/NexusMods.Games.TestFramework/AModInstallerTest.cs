using FluentAssertions;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.DataModel.Entities.Mods;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Games.ArchiveMetadata;
using NexusMods.Abstractions.Games.Downloads;
using NexusMods.Abstractions.Installers;
using NexusMods.Abstractions.Installers.DTO;
using NexusMods.Abstractions.Installers.DTO.Files;
using NexusMods.Abstractions.Installers.Trees;
using NexusMods.Abstractions.IO;
using NexusMods.Abstractions.IO.StreamFactories;
using NexusMods.Abstractions.Serialization;
using NexusMods.Abstractions.Serialization.DataModel;
using NexusMods.DataModel.Extensions;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;

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
        var game = serviceProvider.GetServices<IGame>().OfType<TGame>().Single();
        ModInstaller = game.Installers.OfType<TModInstaller>().Single();
    }

    /// <summary>
    /// Get an auto-incrementing hash so tests don't conflict with each other.
    /// </summary>
    /// <returns></returns>
    protected ulong NextHash()
    {
        return (ulong)Random.Shared.Next();
        //return Interlocked.Decrement(ref nextHash);
    }

    /// <summary>
    /// Gets two auto-incrementing hashes so tests don't conflict with each other.
    /// </summary>
    /// <returns></returns>
    protected (ulong, ulong) Next2Hash()
    {
        return (NextHash(), NextHash());
    }

    /// <summary>
    /// Gets three auto-incrementing hashes so tests don't conflict with each other.
    /// </summary>
    /// <returns></returns>
    protected (ulong, ulong, ulong) Next3Hash()
    {
        return (NextHash(), NextHash(), NextHash());
    }

    /// <summary>
    /// Runs <typeparamref name="TModInstaller"/> and returns all mods to be installed.
    /// </summary>
    /// <param name="archivePath"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    protected async Task<Mod[]> GetModsFromInstaller(
        AbsolutePath archivePath,
        CancellationToken cancellationToken = default)
    {
        var downloadId = await FileOriginRegistry.RegisterDownload(archivePath, new FilePathMetadata
        {
            OriginalName = archivePath.FileName,
            Quality = Quality.Low
        }, cancellationToken);

        var contents = await FileOriginRegistry.Get(downloadId);
        var tree = TreeCreator.Create(contents.Contents, FileStore);

        var install = GameInstallation;
        var info = new ModInstallerInfo()
        {
            ArchiveFiles = tree,
            BaseModId = ModId.NewId(), // unused
            Locations = install.LocationsRegister,
            GameName = install.Game.Name,
            Store = install.Store,
            Version = install.Version,
            ModName = "" // unused
        };

        var results = await ModInstaller.GetModsAsync(info, cancellationToken);
        var mods = results.Select(result => new Mod
        {
            Id = result.Id,
            Files = result.Files.ToEntityDictionary(DataStore),
            Name = result.Name ?? archivePath.FileName,
            Version = result.Version ?? string.Empty,
        }).WithPersist(DataStore).ToArray();

        mods.Should().NotBeEmpty();
        return mods;
    }

    /// <summary>
    /// Runs <typeparamref name="TModInstaller"/> and returns the single mod
    /// from the archive. This calls <see cref="GetModsFromInstaller"/> and
    /// asserts that the installer only returns 1 mod.
    /// </summary>
    /// <param name="archivePath"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    protected async Task<Mod> GetModFromInstaller(
        AbsolutePath archivePath,
        CancellationToken cancellationToken = default)
    {
        var mods = await GetModsFromInstaller(archivePath, cancellationToken);
        mods.Should().ContainSingle();
        return mods.First();
    }

    /// <summary>
    /// Runs <typeparamref name="TModInstaller"/> and returns all mods and files
    /// from the archive in a dictionary. This uses <see cref="GetModsFromInstaller"/>.
    /// </summary>
    /// <param name="archivePath"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    protected async Task<Dictionary<Mod, AModFile[]>> GetModsWithFilesFromInstaller(
        AbsolutePath archivePath,
        CancellationToken cancellationToken = default)
    {
        var mods = await GetModsFromInstaller(archivePath, cancellationToken);
        return mods.ToDictionary(mod => mod, mod => mod.Files.Values.ToArray());
    }

    /// <summary>
    /// Runs <typeparamref name="TModInstaller"/> and returns the single mod and its
    /// files from the archive. This uses <see cref="GetModsFromInstaller"/> and
    /// asserts the installer only returned a single mod.
    /// </summary>
    /// <param name="archivePath"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    protected async Task<(Mod mod, AModFile[] modFiles)> GetModWithFilesFromInstaller(
        AbsolutePath archivePath,
        CancellationToken cancellationToken = default)
    {
        var mods = await GetModsFromInstaller(archivePath, cancellationToken);
        mods.Should().ContainSingle();

        var mod = mods.OrderBy(m => m.Name).First();
        return (mod, mod.Files.Values.ToArray());
    }

    /// <summary>
    /// Helper method to build the archive description and run the installer returning
    /// the metadata of the files to extract. Assumes all files do not have any
    /// FileTypes assigned.
    /// </summary>
    /// <param name="expectedPriority"></param>
    /// <param name="files"></param>
    /// <returns></returns>
    protected Task<IEnumerable<(ulong Hash, LocationId LocationId, string Path)>> BuildAndInstall(
        params (ulong Hash, string Name)[] files)
    {
        return BuildAndInstall(files.Select(f =>
            new ModInstallerExampleFile()
            {
                Name = f.Name,
                Hash = f.Hash,
                Data = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF, 0x42 }
            }
        ));
    }

    /// <summary>
    /// Helper method to build the archive description and run the installer returning
    /// the metadata of the files to extract. Assumes all files do not have a single
    /// IFileAnalysisData assigned.
    /// </summary>
    /// <param name="expectedPriority"></param>
    /// <param name="files"></param>
    /// <returns></returns>
    protected Task<IEnumerable<(ulong Hash, LocationId LocationId, string Path)>> BuildAndInstall(
        params (ulong Hash, string Name, byte[] data)[] files)
    {
        return BuildAndInstall(files.Select(f =>
            new ModInstallerExampleFile
            {
                Name = f.Name,
                Hash = f.Hash,
                Data = f.data
            }
        ));
    }

    /// <summary>
    /// Helper method to build the archive description and run the installer returning
    /// the metadata of the files to extract. Assumes all files do not have a single
    /// FileType assigned.
    /// </summary>
    /// <param name="expectedPriority"></param>
    /// <param name="files"></param>
    /// <returns></returns>
    protected Task<IEnumerable<(ulong Hash, LocationId LocationId, string Path)>> BuildAndInstall(Priority expectedPriority,
        params (ulong Hash, string Name)[] files)
    {
        return BuildAndInstall(files.Select(f => new ModInstallerExampleFile
        {
           Name = f.Name,
           Hash = f.Hash,
           Data = new byte[]{ 0xDE, 0xAD, 0xBE, 0xEF, 0x42}
        }));
    }

    /// <summary>
    /// Helper method to build the archive description and run the installer returning
    /// the metadata of the files to extract. Supplied FileTypes are assigned to the
    /// generated AnalyzedFile instances.
    /// </summary>
    /// <param name="files"></param>
    /// <returns></returns>
    protected async Task<IEnumerable<(ulong Hash, LocationId LocationId, string Path)>> BuildAndInstall(IEnumerable<ModInstallerExampleFile> files)
    {
        ModInstallerResult[] mods;
        var sources = files
            .Select(f => new ModFileTreeSource(f.Hash, (ulong)f.Data.Length, f.Name, new MemoryStreamFactory(f.Name.ToRelativePath(), new MemoryStream(f.Data))))
            .ToArray();

        var tree = ModFileTree.Create(sources);
        var install = GameInstallation;
        var info = new ModInstallerInfo()
        {
            ArchiveFiles = tree,
            BaseModId = ModId.NewId(), // unused
            Locations = install.LocationsRegister,
            GameName = install.Game.Name,
            Store = install.Store,
            Version = install.Version,
            ModName = "" // unused
        };

        mods = (await ModInstaller.GetModsAsync(info)).ToArray();
        if (mods.Length == 0)
            return Array.Empty<(ulong Hash, LocationId LocationId, string Path)>();

        mods.Length.Should().BeGreaterOrEqualTo(1);
        var contents = mods.First().Files;
        return contents.OfType<StoredFile>().Select(m => (m.Hash.Value, m.To.LocationId, m.To.Path.ToString()));
    }
}
