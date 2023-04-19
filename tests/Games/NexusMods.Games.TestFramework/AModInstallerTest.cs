using System.Collections.Immutable;
using FluentAssertions;
using JetBrains.Annotations;
using NexusMods.Common;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.ArchiveContents;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.ModFiles;
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

        var baseMod = new Mod
        {
            Name = archivePath.FileName,
            Files = new EntityDictionary<ModFileId, AModFile>(),
            Id = ModId.New()
        };

        var mods = (await ModInstaller.GetModsAsync(
            GameInstallation,
            baseMod,
            analyzedArchive.Hash,
            analyzedArchive.Contents,
            cancellationToken)).ToArray();

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
    protected Task<IEnumerable<(ulong Hash, GameFolderType FolderType, string Path)>> BuildAndInstall(Priority expectedPriority, 
        params (ulong Hash, string Name)[] files)
    {
        return BuildAndInstall(expectedPriority, files.Select(f => 
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
    protected Task<IEnumerable<(ulong Hash, GameFolderType FolderType, string Path)>> BuildAndInstall(Priority expectedPriority, 
        params (ulong Hash, string Name, IFileAnalysisData? Data)[] files)
    {
        return BuildAndInstall(expectedPriority, files.Select(f => 
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
        return BuildAndInstall(expectedPriority, files.Select(f => new ModInstallerExampleFile()
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
    /// <param name="expectedPriority"></param>
    /// <param name="files"></param>
    /// <returns></returns>
    protected async Task<IEnumerable<(ulong Hash, GameFolderType FolderType, string Path)>>
        BuildAndInstall(Priority expectedPriority, IEnumerable<ModInstallerExampleFile> files)
    {
        var description = BuildArchiveDescription(files);
        
        var priority = ModInstaller.Priority(GameInstallation, description);

        if (expectedPriority == Priority.None)
        {
            priority.Should().Be(expectedPriority, "because the installer doesn't support these files");
            return Array.Empty<(ulong Hash, GameFolderType FolderType, string Path)>();
        }

        priority.Should().Be(expectedPriority, "because the priority should be correct");

        var baseMod = new Mod
        {
            Name = string.Empty,
            Id = ModId.New(),
            Files = new EntityDictionary<ModFileId, AModFile>()
        };

        var mods = (await ModInstaller.GetModsAsync(
            GameInstallation,
            baseMod,
            Hash.From(0xDEADBEEF),
            description)).ToArray();

        mods.Should().ContainSingle();
        var contents = mods.First().Files.Values;
        return contents.OfType<AStaticModFile>().Select(m => (m.Hash.Value, m.To.Type, m.To.Path.ToString().Replace("\\", "/")));
    }
}
