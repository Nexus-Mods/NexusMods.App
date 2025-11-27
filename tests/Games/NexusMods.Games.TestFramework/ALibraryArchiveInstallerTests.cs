using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Text;
using FluentAssertions;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Library.Installers;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Hashing.xxHash3;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
using NexusMods.Paths;
using NexusMods.Sdk;
using NexusMods.Sdk.Library;
using NexusMods.Sdk.Games;
using NexusMods.Sdk.Loadouts;
using Xunit.Abstractions;
using PathTuple = (NexusMods.MnemonicDB.Abstractions.EntityId, NexusMods.Sdk.Games.LocationId, NexusMods.Paths.RelativePath);

namespace NexusMods.Games.TestFramework;

public abstract class ALibraryArchiveInstallerTests<TTest, TGame>(ITestOutputHelper outputHelper) : AIsolatedGameTest<TTest, TGame>(outputHelper)
    where TGame : IGame
{

    /// <summary>
    /// Creates a test archive with the given paths, where each path represents a file whos content is the path itself,
    /// adds it to the library and returns the archive.
    /// </summary>
    protected async Task<LibraryArchive.ReadOnly> AddFromPaths(params string[] paths)
    {
        await using var file = TemporaryFileManager.CreateFile();
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

    
    protected Task<LoadoutItemGroup.ReadOnly> Install<TInstaller>(Loadout.ReadOnly loadout, LibraryArchive.ReadOnly archive)
        where TInstaller : ILibraryArchiveInstaller
    {
        return Install(typeof(TInstaller), loadout, archive);
    }

    protected Task<LoadoutItemGroup.ReadOnly> Install(Type installerType, Loadout.ReadOnly loadout, LibraryArchive.ReadOnly archive)
    {
        var installer = Game.LibraryItemInstallers.FirstOrDefault(t => t.GetType() == installerType);
        installer.Should().NotBeNull();

        return Install(installer!, loadout, archive);
    }

    protected async Task<LoadoutItemGroup.ReadOnly> Install(
        ILibraryItemInstaller installer,
        Loadout.ReadOnly loadout,
        LibraryArchive.ReadOnly archive)
    {
        using var tx = Connection.BeginTransaction();
        var libraryItem = archive.AsLibraryFile().AsLibraryItem();

        var loadoutGroup = new LoadoutItemGroup.New(tx, out var groupId)
        {
            IsGroup = true,
            LoadoutItem = new LoadoutItem.New(tx, groupId)
            {
                Name = libraryItem.Name,
                LoadoutId = loadout,
            },
        };

        var result = await installer.ExecuteAsync(libraryItem, loadoutGroup, tx, loadout, CancellationToken.None);
        result.IsSuccess.Should().BeTrue();

        var dbResult = await tx.Commit();
        var group = dbResult.Remap(loadoutGroup);
        return group;
    }

    /// <summary>
    /// Gets the children of this loadout item as a tuple of from path, hash and game path.
    /// </summary>
    protected IEnumerable<(RelativePath FromPath, Hash Hash, GamePath GamePath)> ChildrenFilesAndHashes(LoadoutItemGroup.ReadOnly group)
    {
        var db = Connection.Db;

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

    public SettingsTask VerifyTx(TxId tx, [CallerFilePath] string sourceFile = "")
    {
        // ReSharper disable once ExplicitCallerInfoArgument
        return Verify(ToTable(Connection.Db.Datoms(tx)), sourceFile: sourceFile);
    }
    
    public string ToTable(IndexSegment datoms)
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
        var sorted = datoms.Resolved(Connection)
            .OrderBy(d => d.E)
            .ThenBy(d => d.A.Id.Name);
        foreach (var datom in sorted)
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
                case PathTuple pathTuple:
                    sb.Append((Remap(pathTuple.Item1).Value.ToString("X16"), pathTuple.Item2, pathTuple.Item3));
                    break;
                case EntityId eid:
                    sb.Append(Remap(eid).Value.ToString("X16").PadRight(48));
                    break;
                case ulong ul:
                    sb.Append(ul.ToString("X16").PadRight(48));
                    break;
                case byte[] byteArray:
                    var code = byteArray.xxHash3().Value;
                    var hash = code.ToString("X16");
                    sb.Append($"Blob 0x{hash} {byteArray.Length} bytes".PadRight(48));
                    break;
                case DateTimeOffset dateTime:
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
    
    protected static IEnumerable<LoadoutFile.ReadOnly> GetFiles(LoadoutItemGroup.ReadOnly group)
    {
        foreach (var loadoutItem in group.Children)
        {
            loadoutItem.TryGetAsLoadoutItemWithTargetPath(out var targetPath).Should().BeTrue();
            targetPath.IsValid().Should().BeTrue();

            targetPath.TryGetAsLoadoutFile(out var loadoutFile).Should().BeTrue();
            loadoutFile.IsValid().Should().BeTrue();

            yield return loadoutFile;
        }
    }
    
    protected static SettingsTask VerifyGroup(LibraryArchive.ReadOnly libraryArchive, LoadoutItemGroup.ReadOnly group, [CallerFilePath] string sourceFile = "")
    {
        var sb = new StringBuilder();

        var paths = GetFiles(group)
            .Select(file =>
            {
                libraryArchive.Children
                    .Where(x => x.AsLibraryFile().Hash == file.Hash)
                    .TryGetFirst(x => x.Path.FileName == file.AsLoadoutItemWithTargetPath().TargetPath.Item3.FileName, out var libraryArchiveFileEntry)
                    .Should().BeTrue();

                libraryArchiveFileEntry.AsLibraryFile().Size.Value.Should().Be(file.Size.Value);
                libraryArchiveFileEntry.IsValid().Should().BeTrue();
                return (libraryArchiveFileEntry, file.AsLoadoutItemWithTargetPath().TargetPath);
            })
            .OrderBy(static targetPath => targetPath.Item2.Item2)
            .ThenBy(static targetPath => targetPath.Item2.Item3)
            .ToArray();

        foreach (var tuple in paths)
        {
            var (libraryArchiveFileEntry, targetPath) = tuple;
            var (_, locationId, path) = targetPath;
            var gamePath = new GamePath(locationId, path);

            sb.AppendLine($"{libraryArchiveFileEntry.AsLibraryFile().Hash} - {libraryArchiveFileEntry.AsLibraryFile().Size}: {libraryArchiveFileEntry.Path} -> {gamePath}");
        }

        var result = sb.ToString();

        // ReSharper disable once ExplicitCallerInfoArgument
        return Verify(result, sourceFile: sourceFile);
    }
}
