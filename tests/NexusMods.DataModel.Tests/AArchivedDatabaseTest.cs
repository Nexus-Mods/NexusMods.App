using System.Diagnostics;
using System.IO.Compression;
using FluentAssertions;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.FileExtractor;

using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Synchronizers.Conflicts;
using NexusMods.Abstractions.NexusModsLibrary;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.Abstractions.Serialization;
using NexusMods.Backend;
using NexusMods.Backend.FileExtractor.FileSignatures;
using NexusMods.Collections;
using NexusMods.CrossPlatform;
using NexusMods.DataModel.SchemaVersions;
using NexusMods.FileExtractor;
using NexusMods.Games.FileHashes;
using NexusMods.Games.StardewValley;
using NexusMods.Hashing.xxHash3;
using NexusMods.MnemonicDB;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Networking.HttpDownloader;
using NexusMods.Networking.NexusWebApi;
using NexusMods.Networking.NexusWebApi.Errors;
using NexusMods.Paths;
using NexusMods.Sdk;
using NexusMods.Sdk.Library;
using NexusMods.Sdk.Games;
using NexusMods.Sdk.Settings;
using NexusMods.StandardGameLocators;
using NexusMods.StandardGameLocators.TestHelpers;
using NSubstitute;
using Xunit.Abstractions;
using Xunit.DependencyInjection;

namespace NexusMods.DataModel.Tests;

/// <summary>
/// A base test class for testing against snapshotted databases.
/// </summary>
public abstract class AArchivedDatabaseTest
{
    private readonly SignatureChecker _zipSignatureChecker = new(FileType.ZIP);
    private readonly ITestOutputHelper _helper;

    protected IServiceProvider ServiceProvider { get; private set; } = null!;

    protected AArchivedDatabaseTest(ITestOutputHelper helper)
    {
        _helper = helper;
    }
    
    protected virtual IServiceCollection AddServices(IServiceCollection services)
    {
        const KnownPath baseKnownPath = KnownPath.EntryDirectory;
        var baseDirectory = $"NexusMods.UI.Tests.Tests-{Guid.NewGuid()}";
        
        var mock = Substitute.For<IGraphQlClient>();
        mock.QueryCollectionId(CollectionSlug.DefaultValue, CancellationToken.None).ReturnsForAnyArgs(callInfo =>
        {
            var slug = callInfo.Arg<CollectionSlug>();
            var id = slug.Value.xxHash3AsUtf8().Value;
            return new GraphQlResult<CollectionId, NotFound>(CollectionId.From(id));
        });

        return services
            .AddDatabaseModels()
            .AddSingleton<TimeProvider>(_ => TimeProvider.System)
            .AddLogging(builder => builder.AddXUnit())
            .AddSerializationAbstractions()
            .AddHttpDownloader()
            .AddSettingsManager()
            .AddGameServices()
            .AddSettings<LoggingSettings>()
            .AddOSInterop()
            .AddRuntimeDependencies()
            .AddRocksDbBackend()
            .AddFileHashes()
            .AddFileSystem()
            .AddDataModel()
            .AddStardewValley()
            .AddLoadoutAbstractions()
            .AddFileExtractors()
            .AddNexusModsCollections()
            .AddJobMonitor()
            .OverrideSettingsForTests<FileHashesServiceSettings>(settings => settings with
            {
                HashDatabaseLocation = new ConfigurablePath(baseKnownPath, $"{baseDirectory}/FileHashService"),
            })
            .AddSingleton<ITestOutputHelperAccessor>(_ => new Accessor { Output = _helper })
            .AddSingleton(mock)
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
        ServiceProvider = services;

        var connection = services.GetRequiredService<IConnection>();
        
        var migrationService = services.GetRequiredService<MigrationService>();
        await migrationService.MigrateAll();

        return new TempConnection
        {
            Host = host,
            Connection = connection,
            TemporaryFileManager = temporaryFileManager,
        };
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
        var databaseFolder = basePath.Combine("Resources/databases");
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
