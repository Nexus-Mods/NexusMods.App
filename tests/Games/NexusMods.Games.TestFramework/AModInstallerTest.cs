using System.Collections.Immutable;
using FluentAssertions;
using JetBrains.Annotations;
using NexusMods.Common;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.ArchiveContents;
using NexusMods.DataModel.Extensions;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.ModFiles;
using NexusMods.DataModel.Loadouts.Mods;
using NexusMods.DataModel.ModInstallers;
using NexusMods.FileExtractor.FileSignatures;
using NexusMods.Hashing.xxHash64;
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
        ModInstaller = serviceProvider.FindImplementationInContainer<TModInstaller, IModInstaller>();
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
        var analyzedArchive = await AnalyzeArchive(archivePath);

        var results = await ModInstaller.GetModsAsync(
            GameInstallation,
            ModId.New(),
            analyzedArchive.Hash,
            analyzedArchive.Contents,
            cancellationToken);

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

        var mod = mods.First();
        return (mod, mod.Files.Values.ToArray());
    }

    /// <summary>
    /// Builds the analyzed archive description for the provided file data, used
    /// to create test data for the <typeparamref name="TModInstaller"/>.
    /// </summary>
    /// <param name="files"></param>
    /// <returns></returns>
    protected EntityDictionary<RelativePath, AnalyzedFile> BuildArchiveDescription(
        IEnumerable<ModInstallerExampleFile> files)
    {
        var description = new EntityDictionary<RelativePath, AnalyzedFile>(DataStore);
        var items = files.Select(file =>
                KeyValuePair.Create(file.Name.ToRelativePath(),
            new AnalyzedFile
            {
                AnalyzersHash = Hash.Zero,
                FileTypes = file.Filetypes,
                Hash = Hash.From(file.Hash),
                Size = Size.From(4),
                AnalysisData = file.AnalysisData.ToImmutableList()
            }));
        return description.With(items);
    }

    /// <summary>
    /// Helper method to build the archive description and run the installer returning
    /// the metadata of the files to extract. Assumes all files do not have any
    /// FileTypes assigned.
    /// </summary>
    /// <param name="expectedPriority"></param>
    /// <param name="files"></param>
    /// <returns></returns>
    protected Task<IEnumerable<(ulong Hash, GameFolderType FolderType, string Path)>> BuildAndInstall(
        params (ulong Hash, string Name)[] files)
    {
        return BuildAndInstall(files.Select(f =>
            new ModInstallerExampleFile()
            {
                Name = f.Name,
                Hash = f.Hash,
                Filetypes = Array.Empty<FileType>()
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
    protected Task<IEnumerable<(ulong Hash, GameFolderType FolderType, string Path)>> BuildAndInstall(
        params (ulong Hash, string Name, IFileAnalysisData? Data)[] files)
    {
        return BuildAndInstall(files.Select(f =>
            new ModInstallerExampleFile()
            {
                Name = f.Name,
                Hash = f.Hash,
                AnalysisData = f.Data == null ? Array.Empty<IFileAnalysisData>() : new[] {f.Data}
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
    protected Task<IEnumerable<(ulong Hash, GameFolderType FolderType, string Path)>> BuildAndInstall(Priority expectedPriority,
        params (ulong Hash, string Name, FileType FileType)[] files)
    {
        return BuildAndInstall(files.Select(f => new ModInstallerExampleFile()
        {
           Name = f.Name,
           Hash = f.Hash,
           Filetypes = new[] {f.FileType}
        }));
    }

    /// <summary>
    /// Helper method to build the archive description and run the installer returning
    /// the metadata of the files to extract. Supplied FileTypes are assigned to the
    /// generated AnalyzedFile instances.
    /// </summary>
    /// <param name="files"></param>
    /// <returns></returns>
    protected async Task<IEnumerable<(ulong Hash, GameFolderType FolderType, string Path)>>
        BuildAndInstall(IEnumerable<ModInstallerExampleFile> files)
    {
        var description = BuildArchiveDescription(files);

        var mods = (await ModInstaller.GetModsAsync(
            GameInstallation,
            ModId.New(),
            Hash.From(0xDEADBEEF),
            description)).ToArray();

        if (mods.Length == 0)
            return Array.Empty<(ulong Hash, GameFolderType FolderType, string Path)>();

        mods.Should().ContainSingle();
        var contents = mods.First().Files;
        return contents.OfType<FromArchive>().Select(m => (m.Hash.Value, m.To.Type, m.To.Path.ToString()));
    }
}
