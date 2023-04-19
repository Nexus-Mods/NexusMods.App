using System.Collections.Immutable;
using FluentAssertions;
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
            priority.Should().Be(expectedPriority,
                "because the installer doesn't support these files");
            return Array
                .Empty<(ulong Hash, GameFolderType FolderType, string Path)>();

        }

        priority.Should().Be(expectedPriority, "because the priority should be correct");

        var contents =
            await ModInstaller.GetFilesToExtractAsync(GameInstallation, Hash.From(0xDEADBEEF), description);
        return contents.OfType<AStaticModFile>().Select(m => (m.Hash.Value, m.To.Type, m.To.Path.ToString().Replace("\\", "/")));
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
