using System.Runtime.CompilerServices;
using FluentAssertions;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.FileStore;
using NexusMods.Abstractions.FileStore.ArchiveMetadata;
using NexusMods.Abstractions.FileStore.Downloads;
using NexusMods.Abstractions.FileStore.Trees;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Installers;
using NexusMods.Abstractions.IO;
using NexusMods.Abstractions.IO.StreamFactories;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Files;
using NexusMods.Abstractions.Loadouts.Ids;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.Abstractions.Serialization.DataModel;
using NexusMods.DataModel.Extensions;
using NexusMods.Games.TestFramework.Verifiers;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;
using Xunit.Sdk;
using File = NexusMods.Abstractions.Loadouts.Files.File;

namespace NexusMods.Games.TestFramework;

[PublicAPI]
public abstract class AModInstallerTest<TGame, TModInstaller> : AGameTest<TGame>, IAsyncLifetime
    where TGame : AGame
    where TModInstaller : IModInstaller
{
    protected readonly TModInstaller ModInstaller;
    protected Loadout.Model Loadout;

    /// <summary>
    /// Constructor.
    /// </summary>
    protected AModInstallerTest(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        var game = serviceProvider.GetServices<IGame>().OfType<TGame>().Single();
        ModInstaller = game.Installers.OfType<TModInstaller>().Single();
    }
    
    public async Task InitializeAsync()
    {
        Loadout = await CreateLoadout();
    }
    
    public async Task DisposeAsync()
    {
        // nothing to do
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
    protected async Task<Mod.Model[]> GetModsFromInstaller(
        AbsolutePath archivePath,
        CancellationToken cancellationToken = default)
    {
        var downloadId = await FileOriginRegistry.RegisterDownload(archivePath, cancellationToken);
        
        var ids = await ArchiveInstaller.AddMods(Loadout.LoadoutId, downloadId, "test", ModInstaller, cancellationToken);
        var db = Connection.Db;
        return ids.Select(id => db.Get<Mod.Model>((EntityId)id)).ToArray();
    }

    /// <summary>
    /// Runs <typeparamref name="TModInstaller"/> and returns the single mod
    /// from the archive. This calls <see cref="GetModsFromInstaller"/> and
    /// asserts that the installer only returns 1 mod.
    /// </summary>
    /// <param name="archivePath"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    protected async Task<Mod.Model> GetModFromInstaller(
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
    protected async Task<Dictionary<Mod.Model, File.Model[]>> GetModsWithFilesFromInstaller(
        AbsolutePath archivePath,
        CancellationToken cancellationToken = default)
    {
        var mods = await GetModsFromInstaller(archivePath, cancellationToken);
        return mods.ToDictionary(mod => mod, mod => mod.Files.ToArray());
    }

    /// <summary>
    /// Runs <typeparamref name="TModInstaller"/> and returns the single mod and its
    /// files from the archive. This uses <see cref="GetModsFromInstaller"/> and
    /// asserts the installer only returned a single mod.
    /// </summary>
    /// <param name="archivePath"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    protected async Task<(Mod.Model mod, File.Model[] modFiles)> GetModWithFilesFromInstaller(
        AbsolutePath archivePath,
        CancellationToken cancellationToken = default)
    {
        var mods = await GetModsFromInstaller(archivePath, cancellationToken);
        mods.Should().ContainSingle();

        var mod = mods.OrderBy(m => m.Name).First();
        return (mod, mod.Files.ToArray());
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
           Data = [0xDE, 0xAD, 0xBE, 0xEF, 0x42]
        }));
    }

    /// <summary>
    /// Helper method to build the archive description and run the installer returning
    /// the metadata of the files to extract. Supplied FileTypes are assigned to the
    /// generated AnalyzedFile instances.
    /// </summary>
    /// <param name="files"></param>
    /// <returns></returns>
    protected async Task<IEnumerable<(ulong Hash, LocationId LocationId, string Path)>> 
        BuildAndInstall(IEnumerable<ModInstallerExampleFile> files)
    {
        ModInstallerResult[] mods;
        var sources = files
            .Select(f => new ModFileTreeSource(f.Hash, (ulong)f.Data.Length, f.Name, new MemoryStreamFactory(f.Name.ToRelativePath(), new MemoryStream(f.Data))))
            .ToArray();

        var tree = ModFileTree.Create(sources);
        var install = GameInstallation;

        ModId baseId;
        {
            using var tx = Connection.BeginTransaction();
            var mod = new Mod.Model(tx)
            {
                Name = "Base Mod (Test)",
                Category = ModCategory.Mod,
                Status = ModStatus.Installing,
            };
            var result = await tx.Commit();
            baseId = result.Remap(mod).ModId;
        }
        
        var info = new ModInstallerInfo
        {
            ArchiveFiles = tree,
            BaseModId = baseId,
            Locations = install.LocationsRegister,
            GameName = install.Game.Name,
            Store = install.Store,
            Version = install.Version,
            ModName = "",
            Source = Connection.Db.Get<DownloadAnalysis.Model>(EntityId.From(0))
        };

        mods = (await ModInstaller.GetModsAsync(info)).ToArray();
        if (mods.Length == 0)
            return Array.Empty<(ulong Hash, LocationId LocationId, string Path)>();

        mods.Length.Should().BeGreaterOrEqualTo(1);
        var contents = mods.First().Files;
        return contents.Select(m => (m.GetFirst(StoredFile.Hash).Value, m.GetFirst(File.To).LocationId, m.GetFirst(File.To).Path.ToString()));
    }

    protected async Task VerifyMod(Mod.Model mod, [CallerFilePath] string sourceFile = "")
    {
        var res = VerifiableMod.From(mod);

        // ReSharper disable once ExplicitCallerInfoArgument
        await Verify(res, sourceFile: sourceFile);
    }

    protected async Task VerifyMods(Mod.Model[] mods, [CallerFilePath] string sourceFile = "")
    {
        var res = mods
            .Select(VerifiableMod.From)
            .OrderByDescending(mod => mod.Name)
            .ThenByDescending(mod => mod.Files.Count)
            .ToArray();

        // ReSharper disable once ExplicitCallerInfoArgument
        await Verify(res, sourceFile: sourceFile);
    }
}
