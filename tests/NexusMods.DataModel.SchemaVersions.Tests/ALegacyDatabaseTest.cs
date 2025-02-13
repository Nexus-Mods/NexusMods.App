using System.Diagnostics;
using System.IO.Compression;
using FluentAssertions;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.FileExtractor;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.NexusModsLibrary;
using NexusMods.Abstractions.Serialization;
using NexusMods.Abstractions.Settings;
using NexusMods.App.BuildInfo;
using NexusMods.Collections;
using NexusMods.CrossPlatform;
using NexusMods.FileExtractor;
using NexusMods.FileExtractor.FileSignatures;
using NexusMods.Games.FileHashes;
using NexusMods.Games.Generic;
using NexusMods.Games.StardewValley;
using NexusMods.Games.TestFramework;
using NexusMods.MnemonicDB;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Storage;
using NexusMods.MnemonicDB.Storage.Abstractions;
using NexusMods.MnemonicDB.Storage.RocksDbBackend;
using NexusMods.Networking.Downloaders;
using NexusMods.Networking.HttpDownloader;
using NexusMods.Networking.Steam;
using NexusMods.Paths;
using NexusMods.Settings;
using NexusMods.StandardGameLocators;
using NexusMods.StandardGameLocators.TestHelpers;
using Xunit.Abstractions;
using Xunit.DependencyInjection;

namespace NexusMods.DataModel.SchemaVersions.Tests;

public abstract class ALegacyDatabaseTest
{
    private readonly SignatureChecker _zipSignatureChecker = new(FileType.ZIP);
    private readonly ITestOutputHelper _helper;

    protected ALegacyDatabaseTest(ITestOutputHelper helper)
    {
        _helper = helper;
    }
    
    protected virtual IServiceCollection AddServices(IServiceCollection services)
    {
        
        // So Gog doesn't exist on linux, but this is a test where we're stubbing out the games
        // so we don't actually need gog, and setting up Heroic and all the other wine stuff 
        // just to run this test is a pain. OSX does have GOG but we don't have support for it yet
        if (OSInformation.Shared.IsLinux || OSInformation.Shared.IsOSX)
            services.AddSingleton<IGameLocator, GogLocator>();
        
        const KnownPath baseKnownPath = KnownPath.EntryDirectory;
        var baseDirectory = $"NexusMods.UI.Tests.Tests-{Guid.NewGuid()}";
        
        return services
            .AddLogging(builder => builder.AddXUnit())
            .AddSerializationAbstractions()
            .AddHttpDownloader()
            .AddDownloaders()
            .AddSettingsManager()
            .AddSettings<LoggingSettings>()
            .AddCrossPlatform()
            .AddRocksDbBackend()
            .AddFileHashes()
            .AddFileSystem()
            .AddDataModel()
            .AddLibraryModels()
            .AddStardewValley()
            .AddLoadoutAbstractions()
            .AddFileExtractors()
            .AddStubbedStardewValley()
            .AddNexusModsCollections()
            .AddNexusModsLibraryModels()
            .OverrideSettingsForTests<FileHashesServiceSettings>(settings => settings with
            {
                HashDatabaseLocation = new ConfigurablePath(baseKnownPath, $"{baseDirectory}/FileHashService"),
            })
            .AddStandardGameLocators(registerConcreteLocators:false, registerHeroic:false, registerWine: false)
            .AddSingleton<ITestOutputHelperAccessor>(_ => new Accessor { Output = _helper })
            .Validate();
    }
    
    private class Accessor : ITestOutputHelperAccessor
    {
        public ITestOutputHelper? Output { get; set; }
    }

    public record TempConnection : IAsyncDisposable
    {
        public required IHost Host { get; init; }
        public required TemporaryFileManager TemporaryFileManager { get; init; }
        public required IConnection Connection { get; init; }
        public required MigrationId OldId { get; init; }
        
        public void Dispose()
        {
            Host.StopAsync().GetAwaiter().GetResult();
            TemporaryFileManager.Dispose();
        }

        public async ValueTask DisposeAsync()
        {
            await CastAndDispose(Host);
            await CastAndDispose(TemporaryFileManager);

            return;

            static async ValueTask CastAndDispose(IDisposable resource)
            {
                try
                {
                    if (resource is IAsyncDisposable resourceAsyncDisposable)
                        await resourceAsyncDisposable.DisposeAsync();
                    else
                        resource.Dispose();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Failed to dispose resource: " + ex);
                }
            }
        }
    }
    
    [MustDisposeResource]
    public async Task<TempConnection> ConnectionFor(string name)
    {
        
        var path = DatabaseFolder().Combine(name); 
        path.FileExists.Should().BeTrue("the database file should exist");
        
        await using (var stream = path.Read())
        {
            var isZip = await _zipSignatureChecker.MatchesAnyAsync(stream);
            isZip.Should().BeTrue("the database file should be a ZIP archive, you may need to pull the file from LFS (`git lfs pull`)");
        }

        var basePath = FileSystem.Shared.GetKnownPath(KnownPath.EntryDirectory).Combine("Temp").Combine($"Test-{Guid.NewGuid()}");
        basePath.CreateDirectory();
        basePath.DirectoryExists().Should().BeTrue();

        var temporaryFileManager = new TemporaryFileManager(FileSystem.Shared, basePath: basePath);

        var workingFolder = temporaryFileManager.CreateFolder();
        
        ZipFile.ExtractToDirectory(path.ToString(), workingFolder.Path.ToString());
        
        var datamodelFolder = workingFolder.Path.Combine("MnemonicDB.rocksdb");
        datamodelFolder.DirectoryExists().Should().BeTrue("the extracted database folder should exist");
        datamodelFolder.EnumerateFiles().Should().NotBeEmpty("the extracted database folder should contain files");
        
        var host = new HostBuilder()
            .ConfigureServices(s =>
                {
                    AddServices(s);
                    s.AddDatomStoreSettings(new DatomStoreSettings
                        {
                            Path = datamodelFolder
                        }
                    );
                }
            )
            .Build();
        
        await host.StartAsync();
        
        var services = host.Services;
        
        var connection = services.GetRequiredService<IConnection>();
        
        var oldMigrationId = RecordedVersion(connection.Db);

        var migrationService = services.GetRequiredService<MigrationService>();
        await migrationService.MigrateAll();

        return new TempConnection
        {
            Host = host,
            OldId = oldMigrationId,
            Connection = connection,
            TemporaryFileManager = temporaryFileManager,
        };
    }

    private MigrationId RecordedVersion(IDb db)
    {
        var cache = db.AttributeCache;
        if (!cache.Has(SchemaVersion.CurrentVersion.Id))
            return MigrationId.From(0);
        
        var fingerprints = db.Datoms(SchemaVersion.CurrentVersion);
        if (fingerprints.Count == 0)
            return MigrationId.From(0);
        return (MigrationId)db.Datoms(SchemaVersion.CurrentVersion).Single().Resolved(db.Connection.AttributeResolver).ObjectValue;
    }

    private Statistics GetStatistics(IDb db, string name, MigrationId oldId)
    {
        var timestampAttr = MnemonicDB.Abstractions.BuiltInEntities.Transaction.Timestamp;
        
        var timestamp = (DateTimeOffset)db.Get(PartitionId.Transactions.MakeEntityId(1)).Resolved(db.Connection).First(t => t.A == timestampAttr).ObjectValue;
        
        return new Statistics
        {
            Name = name,
            OldId = oldId.Value,
            NewId = RecordedVersion(db).Value,
            Loadouts = Loadout.All(db).Count,
            LoadoutItemGroups = LoadoutItemGroup.All(db).Count,
            Files = LoadoutItemWithTargetPath.All(db).Count,
            Collections = CollectionGroup.All(db).Count,
            Created = timestamp.ToString("yyyy-MM-dd HH:mm:ss")
        };
    }

    /// <summary>
    /// Statistics about the data in a database
    /// </summary>
    record Statistics
    {
        public string Name { get; init; }
        
        public ushort OldId { get; init; }
        
        public ushort NewId{ get; init; }
        
        public int Loadouts { get; init; }
        public int LoadoutItemGroups { get; init; }
        public int Files { get; init; }
        public int Collections { get; init; }
        public string Created { get; init; }
    }
    
    public static IEnumerable<object[]> DatabaseNames()
    {
        var databaseFolder = DatabaseFolder();
        var files = databaseFolder.EnumerateFiles("*.zip").Order(AbsolutePathComparer.Instance).ToArray();
        foreach (var file in files)
        {
            yield return [file.Name];
        }
    }

    protected static AbsolutePath DatabaseFolder()
    {
        var basePath = FileSystem.Shared.GetKnownPath(KnownPath.EntryDirectory).Parent.Parent.Parent;
        var databaseFolder = basePath.Combine("Resources/Databases");
        return databaseFolder;
    }
}

file class AbsolutePathComparer : IComparer<AbsolutePath>
{
    public static IComparer<AbsolutePath> Instance => new AbsolutePathComparer();

    public int Compare(AbsolutePath x, AbsolutePath y)
    {
        return string.Compare(x.ToString(), y.ToString(), StringComparison.OrdinalIgnoreCase);
    }
}
