using System.Text;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.IO;
using NexusMods.Abstractions.IO.StreamFactories;
using NexusMods.Abstractions.Settings;
using NexusMods.App.GarbageCollection.Nx;
using NexusMods.DataModel;
using NexusMods.Hashing.xxHash64;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;
namespace NexusMods.App.GarbageCollection.DataModel.Tests;

public class DataStoreNxArchiveFinderTests(NxFileStore fileStore, IConnection connection, ISettingsManager settingsManager)
{
    private readonly NxFileStore _fileStore = fileStore;
    private readonly IConnection _connection = connection;
    private readonly ISettingsManager _settingsManager = settingsManager;

    [Fact]
    public async Task FindAllArchives_ShouldDetectAllCreatedArchives()
    {
        // Arrange
        var archiveContents = new[]
        {
            [
                ("file1.txt", "Content 1"),
                ("file2.txt", "Content 2"),
            ],
            [
                ("file3.txt", "Content 3"),
                ("file4.txt", "Content 4"),
            ],
            new[]
            {
                ("file5.txt", "Content 5"),
                ("file6.txt", "Content 6"),
            }
        };

        var createdArchives = new List<AbsolutePath>();
        foreach (var files in archiveContents)
        {
            var records = CreateArchivedFileEntries(files);
            await _fileStore.BackupFiles(records);
            createdArchives.Add(GetArchivePath(records[0].Hash));
        }

        // Act
        var archiveGc = new ArchiveGarbageCollector<NxParsedHeaderState, FileEntryWrapper>();
        DataStoreNxArchiveFinder.FindAllArchives(_settingsManager, archiveGc);

        // Assert
        foreach (var createdArchive in createdArchives)
            archiveGc.AllArchives.Should().Contain(reference => reference.FilePath == createdArchive, $"The created archive {createdArchive} should be detected");
    }

    private static List<ArchivedFileEntry> CreateArchivedFileEntries(IEnumerable<(string fileName, string content)> files)
    {
        var records = new List<ArchivedFileEntry>();
        foreach (var (fileName, content) in files)
        {
            var data = Encoding.UTF8.GetBytes(content);
            var hash = data.AsSpan().XxHash64();

            var entry = new ArchivedFileEntry(
                new MemoryStreamFactory(fileName.ToRelativePath(), new MemoryStream(data)),
                hash,
                Size.FromLong(data.Length)
            );

            records.Add(entry);
        }

        return records;
    }

    private AbsolutePath GetArchivePath(Hash hash)
    {
        _fileStore.TryGetLocation(_connection.Db, hash, null, out var archivePath, out _).Should().BeTrue("Archive should exist");
        return archivePath;
    }
    
    public class Startup
    {
        // https://github.com/pengweiqhca/Xunit.DependencyInjection?tab=readme-ov-file#3-closest-startup
        // A trick for parallelizing tests with Xunit.DependencyInjection
        public void ConfigureServices(IServiceCollection services) => DIHelpers.ConfigureServices(services);
    }
}
