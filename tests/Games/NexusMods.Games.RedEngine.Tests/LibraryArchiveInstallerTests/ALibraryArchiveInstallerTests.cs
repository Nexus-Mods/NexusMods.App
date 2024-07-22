using System.IO.Compression;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Jobs;
using NexusMods.Abstractions.Library;
using NexusMods.Abstractions.Library.Installers;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Extensions.Hashing;
using NexusMods.Games.TestFramework;
using NexusMods.Hashing.xxHash64;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;

namespace NexusMods.Games.RedEngine.Tests.LibraryArchiveInstallerTests;

public abstract class ALibraryArchiveInstallerTests<TInstaller> : AGameTest<Cyberpunk2077.Cyberpunk2077Game>
    where TInstaller : ALibraryArchiveInstaller
{
    private readonly ILibraryService _libraryService;
    private readonly TemporaryFileManager _tempFileManager;

    protected ALibraryArchiveInstallerTests(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        _libraryService = serviceProvider.GetRequiredService<ILibraryService>();
        _tempFileManager = serviceProvider.GetRequiredService<TemporaryFileManager>();
    }

    /// <summary>
    /// Creates a test archive with the given paths, where each path represents a file whos content is the path itself,
    /// adds it to the library and returns the archive.
    /// </summary>
    protected async Task<LibraryArchive.ReadOnly> AddFromPaths(params string[] paths)
    {
        await using var file = _tempFileManager.CreateFile();
        {
            await using var archiveStream = file.Path.Create();
            using var zip = new ZipArchive(archiveStream, ZipArchiveMode.Create);
            foreach (var path in paths)
            {
                var entry = zip.CreateEntry(path);
                await using var stream = entry.Open();
                await stream.WriteAsync(Encoding.UTF8.GetBytes(path));
            }
        }

        var archiveHash = await file.Path.XxHash64Async();
        
        var job = _libraryService.AddLocalFile(file.Path);
        await job.StartAsync();
        var result = await job.WaitToFinishAsync();
        
        if (!result.TryGetCompleted(out var completed))
            throw new InvalidOperationException("The job should have completed successfully.");
        
        if (!completed.TryGetData<LocalFile.ReadOnly>(out var item))
            throw new InvalidOperationException("The job should have returned a local file.");
        
        item.AsLibraryFile().Hash.Should().Be(archiveHash, "The hash of the library file should match the hash of the archive.");

        return LibraryFile.FindByHash(Connection.Db, archiveHash).OfTypeLibraryArchive().First();
    }

    protected async Task<LoadoutItem.ReadOnly[]> Install(Loadout.ReadOnly loadout, LibraryArchive.ReadOnly archive)
    {
        var installer = Game.Installers.OfType<TInstaller>().FirstOrDefault();
        if (installer == null)
            throw new InvalidOperationException($"No installer of type {typeof(TInstaller).Name} found for game {Game.Name}.");

        using var tx = Connection.BeginTransaction();
        var results = await installer.ExecuteAsync(archive, tx, loadout, CancellationToken.None);
        
        results.Length.Should().BePositive("The installer should have installed at least one file.");
        
        var dbResult = await tx.Commit();
        
        return results.Select(item => dbResult.Remap(item)).ToArray();
    }

    /// <summary>
    /// Gets the children of this loadout item as a tuple of from path, hash and game path.
    /// </summary>
    public IEnumerable<(RelativePath FromPath, Hash Hash, GamePath GamePath)> ChildrenFilesAndHashes(LoadoutItem.ReadOnly item)
    {

        var db = Connection.Db;
        
        if (!item.TryGetAsLoadoutItemGroup(out var group))
            throw new InvalidOperationException("The item should be a group.");

        foreach (var child in group.Children)
        {
            if (!child.TryGetAsLoadoutItemWithTargetPath(out var itemWithTargetPath))
                throw new InvalidOperationException("The child should be an item with a target path.");
            
            if (!itemWithTargetPath.TryGetAsLoadoutFile(out var file))
                throw new InvalidOperationException("The child should be a file.");

            var libraryFile = LibraryFile.FindByHash(db, file.Hash).First();
            yield return (libraryFile.FileName, libraryFile.Hash, itemWithTargetPath.TargetPath);
        }
    }
}
