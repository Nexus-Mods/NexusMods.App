using System.IO.Compression;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Library;
using NexusMods.Abstractions.Library.Installers;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Extensions.Hashing;
using NexusMods.Hashing.xxHash64;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
using NexusMods.Paths;

namespace NexusMods.Games.TestFramework;

public abstract class ALibraryArchiveInstallerTests<TGame> : AGameTest<TGame>
where TGame : AGame
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

        return await RegisterLocalArchive(file);
    }

    public async Task<LibraryArchive.ReadOnly> RegisterLocalArchive(AbsolutePath file)
    {
        var archiveHash = await file.XxHash64Async();
        
        var job = _libraryService.AddLocalFile(file);
        await job.StartAsync();
        var result = await job.WaitToFinishAsync();
        
        if (!result.TryGetCompleted(out var completed))
            throw new InvalidOperationException("The job should have completed successfully.");
        
        if (!completed.TryGetData<LocalFile.ReadOnly>(out var item))
            throw new InvalidOperationException("The job should have returned a local file.");
        
        item.AsLibraryFile().Hash.Should().Be(archiveHash, "The hash of the library file should match the hash of the archive.");

        return LibraryFile.FindByHash(Connection.Db, archiveHash).OfTypeLibraryArchive().First();
    }

    protected Task<LoadoutItem.ReadOnly[]> Install<TInstaller>(Loadout.ReadOnly loadout, LibraryArchive.ReadOnly archive)
        where TInstaller : ILibraryArchiveInstaller
    {
        return Install(typeof(TInstaller), loadout, archive);
    }
    
    protected async Task<LoadoutItem.ReadOnly[]> Install(Type installerType, Loadout.ReadOnly loadout, LibraryArchive.ReadOnly archive)
    {
        var installer = Game.LibraryItemInstallers.FirstOrDefault(t => t.GetType() == installerType);
        installer.Should().NotBeNull();

        using var tx = Connection.BeginTransaction();
        var results = await installer!.ExecuteAsync(archive.AsLibraryFile().AsLibraryItem(), tx, loadout, CancellationToken.None);
        
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

        foreach (var child in group.Children.OrderBy(child => child.Name))
        {
            if (!child.TryGetAsLoadoutItemWithTargetPath(out var itemWithTargetPath))
                throw new InvalidOperationException("The child should be an item with a target path.");
            
            if (!itemWithTargetPath.TryGetAsLoadoutFile(out var file))
                throw new InvalidOperationException("The child should be a file.");

            var libraryFile = LibraryFile.FindByHash(db, file.Hash).First();
            if (libraryFile.TryGetAsLibraryArchiveFileEntry(out var entry )) 
                yield return (entry.Path, libraryFile.Hash, itemWithTargetPath.TargetPath);
            else 
                yield return (libraryFile.FileName, libraryFile.Hash, itemWithTargetPath.TargetPath);
        }
    }

    public SettingsTask VerifyTx(TxId tx)
    {
        return Verify(ToTable(Connection.Db.Datoms(tx)));
    }
    
    public static string ToTable(IndexSegment datoms)
    {

        string TruncateOrPad(string val, int length)
        {
            if (val.Length > length)
            {
                var midPoint = length / 2;
                return (val[..(midPoint - 2)] + "..." + val[^(midPoint - 2)..]).PadRight(length);
            }

            return val.PadRight(length);
        }

        var remaps = new Dictionary<EntityId, EntityId>();
        
        // Makes all entity Ids local to this table. This allows us to run several tests and the results of one
        // result won't clobber the others
        EntityId Remap(EntityId id)
        {
            if (remaps.TryGetValue(id, out var remapped)) 
                return remapped;
            
            remapped = PartitionId.Entity.MakeEntityId((ulong)remaps.Count);
            remaps.Add(id, remapped);

            return remapped;
        }

        var dateTimeCount = 0;

        var sb = new StringBuilder();
        foreach (var datom in datoms.Resolved())
        {
            var isRetract = datom.IsRetract;

            var symColumn = TruncateOrPad(datom.A.Id.Name, 24);
            sb.Append(isRetract ? "-" : "+");
            sb.Append(" | ");
            sb.Append(Remap(datom.E).Value.ToString("X16"));
            sb.Append(" | ");
            sb.Append(symColumn);
            sb.Append(" | ");



            switch (datom.ObjectValue)
            {
                case EntityId eid:
                    sb.Append(Remap(eid).Value.ToString("X16").PadRight(48));
                    break;
                case ulong ul:
                    sb.Append(ul.ToString("X16").PadRight(48));
                    break;
                case byte[] byteArray:
                    var code = byteArray.XxHash64().Value;
                    var hash = code.ToString("X16");
                    sb.Append($"Blob 0x{hash} {byteArray.Length} bytes".PadRight(48));
                    break;
                case DateTime dateTime:
                    sb.Append($"DateTime : {dateTimeCount++}".PadRight(48));
                    break;
                default:
                    sb.Append(TruncateOrPad(datom.ObjectValue.ToString()!, 48));
                    break;
            }

            sb.Append(" | ");
            sb.Append(Remap(EntityId.From(datom.T.Value)).Value.ToString("X16"));

            sb.AppendLine();
        }

        return sb.ToString();
    }
}
