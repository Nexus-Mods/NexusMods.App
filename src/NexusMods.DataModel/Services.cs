using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Settings;
using NexusMods.Abstractions.Diagnostics;
using NexusMods.Abstractions.DiskState;
using NexusMods.Abstractions.FileStore;
using NexusMods.Abstractions.FileStore.ArchiveMetadata;
using NexusMods.Abstractions.FileStore.Downloads;
using NexusMods.Abstractions.Games.Loadouts;
using NexusMods.Abstractions.Games.Loadouts.Sorting;
using NexusMods.Abstractions.Installers;
using NexusMods.Abstractions.IO;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Messaging;
using NexusMods.Abstractions.Serialization;
using NexusMods.Abstractions.Serialization.ExpressionGenerator;
using NexusMods.DataModel.ArchiveContents;
using NexusMods.DataModel.Attributes;
using NexusMods.DataModel.CommandLine.Verbs;
using NexusMods.DataModel.Diagnostics;
using NexusMods.DataModel.JsonConverters;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.LoadoutSynchronizerDTOs;
using NexusMods.DataModel.Messaging;
using NexusMods.DataModel.Settings;
using NexusMods.DataModel.Sorting;
using NexusMods.DataModel.TriggerFilter;
using NexusMods.Extensions.DependencyInjection;
using NexusMods.MnemonicDB;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Storage;
using NexusMods.MnemonicDB.Storage.Abstractions;
using NexusMods.Paths;

namespace NexusMods.DataModel;

/// <summary/>
public static class Services
{
    /// <summary>
    /// Adds all services related to the <see cref="DataModel"/> to your dependency
    /// injection container.
    /// </summary>
    public static IServiceCollection AddDataModel(this IServiceCollection coll)
    {

        coll.AddMnemonicDB();
        coll.AddMnemonicDBStorage();

        coll.AddSettings<DataModelSettings>();
        coll.AddSettingsStorageBackend<DataStoreSettingsBackend>(isDefault: true);

        coll.AddSingleton<MnemonicDB.Storage.InMemoryBackend.Backend>();
        coll.AddSingleton<MnemonicDB.Storage.RocksDbBackend.Backend>();

        coll.AddSingleton<DatomStoreSettings>(sp =>
            {
                var fileSystem = sp.GetRequiredService<IFileSystem>();
                var settingsManager = sp.GetRequiredService<ISettingsManager>();
                var settings = settingsManager.Get<DataModelSettings>();
                return new DatomStoreSettings
                {
                    Path = settings.MnemonicDBPath.ToPath(fileSystem),
                };
            }
        );
        
        coll.AddSingleton<IStoreBackend>(sp =>
        {
            var settingsManager = sp.GetRequiredService<ISettingsManager>();
            var settings = settingsManager.Get<DataModelSettings>();
            if (settings.UseInMemoryDataModel)
            {
                return sp.GetRequiredService<MnemonicDB.Storage.InMemoryBackend.Backend>();
            }
            else
            {
                var datomStoreSettings = sp.GetRequiredService<DatomStoreSettings>();

                if (!datomStoreSettings.Path.DirectoryExists()) 
                    datomStoreSettings.Path.CreateDirectory();
                
                return sp.GetRequiredService<MnemonicDB.Storage.RocksDbBackend.Backend>();
            }
        });
        
        coll.AddSingleton<MessageBus>();
        coll.AddSingleton(typeof(IMessageConsumer<>), typeof(MessageConsumer<>));
        coll.AddSingleton(typeof(IMessageProducer<>), typeof(MessageProducer<>));

        coll.AddSingleton<JsonConverter, AbsolutePathConverter>();
        coll.AddSingleton<JsonConverter, RelativePathConverter>();
        coll.AddSingleton<JsonConverter, GamePathConverter>();
        coll.AddSingleton<JsonConverter, DateTimeConverter>();
        coll.AddSingleton<JsonConverter, SizeConverter>();


        coll.AddAllSingleton<IDataStore, SqliteDataStore>();
        
        // File Store
        coll.AddAttributeCollection(typeof(ArchivedFileContainer));
        coll.AddAttributeCollection(typeof(ArchivedFile));
        coll.AddAllSingleton<IFileStore, NxFileStore>();

        coll.AddSingleton(typeof(IFingerprintCache<,>), typeof(DataStoreFingerprintCache<,>));
        
        coll.AddAllSingleton<IArchiveInstaller, ArchiveInstaller>();
        coll.AddAllSingleton<IToolManager, ToolManager>();
        
        // Disk State Registry
        coll.AddAllSingleton<IDiskStateRegistry, DiskStateRegistry>();
        coll.AddAttributeCollection(typeof(DiskState));
        coll.AddAttributeCollection(typeof(InitialDiskState));

        // File Hash Cache
        coll.AddAllSingleton<IFileHashCache, FileHashCache>();
        coll.AddAttributeCollection(typeof(HashCacheEntry));
        
        coll.AddAllSingleton<IApplyService, ApplyService>();

        coll.AddSingleton<ITypeFinder>(_ => new AssemblyTypeFinder(typeof(Services).Assembly));
        coll.AddAllSingleton<ISorter, Sorter>();
        
        // Download Analyzer
        coll.AddAttributeCollection(typeof(DownloadAnalysis));
        coll.AddAttributeCollection(typeof(DownloadContentEntry));
        coll.AddAttributeCollection(typeof(FilePathMetadata));
        coll.AddAllSingleton<IFileOriginRegistry, FileOriginRegistry>();


        // Diagnostics
        coll.AddAllSingleton<IDiagnosticManager, DiagnosticManager>();
        coll.AddSettings<DiagnosticSettings>();
        
        // Verbs
        coll.AddLoadoutManagementVerbs()
            .AddToolVerbs()
            .AddFileHashCacheVerbs()
            .AddArchiveVerbs();

        return coll;
    }
}
