using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Settings;
using NexusMods.Abstractions.Diagnostics;
using NexusMods.Abstractions.DiskState;
using NexusMods.Abstractions.FileStore;
using NexusMods.Abstractions.FileStore.ArchiveMetadata;
using NexusMods.Abstractions.FileStore.Downloads;
using NexusMods.Abstractions.FileStore.Nx.Models;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games.Loadouts.Sorting;
using NexusMods.Abstractions.GC;
using NexusMods.Abstractions.IO;
using NexusMods.Abstractions.Jobs;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.MnemonicDB.Analyzers;
using NexusMods.Abstractions.Resources.DB;
using NexusMods.Abstractions.Serialization.ExpressionGenerator;
using NexusMods.DataModel.CommandLine.Verbs;
using NexusMods.DataModel.Diagnostics;
using NexusMods.DataModel.JsonConverters;
using NexusMods.DataModel.Settings;
using NexusMods.DataModel.Sorting;
using NexusMods.DataModel.Synchronizer;
using NexusMods.Extensions.DependencyInjection;
using NexusMods.MnemonicDB;
using NexusMods.MnemonicDB.Abstractions;
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
        coll.AddAnalyzers();

        // Settings
        coll.AddSettings<DataModelSettings>();
        coll.AddSettingsStorageBackend<MnemonicDBSettingsBackend>(isDefault: true);
        coll.AddAttributeCollection(typeof(Setting));

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

        coll.AddSingleton<JsonConverter, AbsolutePathConverter>();
        coll.AddSingleton<JsonConverter, RelativePathConverter>();
        coll.AddSingleton<JsonConverter, GamePathConverter>();
        coll.AddSingleton<JsonConverter, DateTimeConverter>();
        coll.AddSingleton<JsonConverter, SizeConverter>();
        
        // Game Registry
        coll.AddSingleton<IGameRegistry, GameRegistry>();
        coll.AddHostedService(s => (GameRegistry)s.GetRequiredService<IGameRegistry>());
        coll.AddAttributeCollection(typeof(GameInstallMetadata));
        
        // File Store
        coll.AddAttributeCollection(typeof(ArchivedFileContainer));
        coll.AddAttributeCollection(typeof(ArchivedFile));
        coll.AddAllSingleton<IFileStore, NxFileStore>();
        
        coll.AddAllSingleton<IToolManager, ToolManager>();

        // Disk State and Synchronizer
        coll.AddDiskStateEntryModel();
        coll.AddAllSingleton<ISynchronizerService, SynchronizerService>();

        coll.AddSingleton<ITypeFinder>(_ => new AssemblyTypeFinder(typeof(Services).Assembly));
        coll.AddAllSingleton<ISorter, Sorter>();
        
        // Download Analyzer
        coll.AddAttributeCollection(typeof(DownloadAnalysis));
        coll.AddAttributeCollection(typeof(DownloadContentEntry));
        coll.AddAttributeCollection(typeof(FilePathMetadata));
        coll.AddAttributeCollection(typeof(StreamBasedFileOriginMetadata));
        
        // Diagnostics
        coll.AddAllSingleton<IDiagnosticManager, DiagnosticManager>();
        coll.AddSettings<DiagnosticSettings>();
        
        // GC
        coll.AddAllSingleton<IGarbageCollectorRunner, GarbageCollectorRunner>();

        coll.AddPersistedDbResourceModel();

        // Verbs
        coll.AddLoadoutManagementVerbs()
            .AddImportExportVerbs()
            .AddToolVerbs();

        return coll;
    }
}
