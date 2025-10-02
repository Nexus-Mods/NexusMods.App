using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Sdk.Settings;
using NexusMods.Abstractions.Diagnostics;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.GC;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Sorting;
using NexusMods.Abstractions.Serialization.ExpressionGenerator;
using NexusMods.DataModel.CommandLine.Verbs;
using NexusMods.DataModel.Diagnostics;
using NexusMods.DataModel.JsonConverters;
using NexusMods.DataModel.SchemaVersions;
using NexusMods.DataModel.Sorting;
using NexusMods.DataModel.Synchronizer;
using NexusMods.DataModel.Undo;
using NexusMods.HyperDuck.Adaptor.Impls.ValueAdaptor;
using NexusMods.MnemonicDB;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Storage.Abstractions;
using NexusMods.MnemonicDB.Storage.RocksDbBackend;
using NexusMods.Paths;
using NexusMods.Sdk;
using NexusMods.Sdk.FileStore;
using NexusMods.Sdk.Resources;

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
        coll.AddMigrations();

        coll.AddAmbientQueriesSql();
        coll.AddValueAdaptor<ushort, LocationId>(LocationId.From);

        // Settings
        coll.AddSettings<DataModelSettings>();

        coll.AddSingleton<DatomStoreSettings>(sp =>
            {
                var fileSystem = sp.GetRequiredService<IFileSystem>();
                var settingsManager = sp.GetRequiredService<ISettingsManager>();
                var settings = settingsManager.Get<DataModelSettings>();
                if (settings.UseInMemoryDataModel)
                    return DatomStoreSettings.InMemory;
                
                var path = settings.MnemonicDBPath.ToPath(fileSystem);
                if (!path.DirectoryExists())
                    path.CreateDirectory();
                return new DatomStoreSettings
                {
                    Path = settings.MnemonicDBPath.ToPath(fileSystem),
                };
            }
        );
        
        coll.AddSingleton<IStoreBackend>(_ => new Backend());

        coll.AddSingleton<JsonConverter, AbsolutePathConverter>();
        coll.AddSingleton<JsonConverter, RelativePathConverter>();
        coll.AddSingleton<JsonConverter, GamePathConverter>();
        coll.AddSingleton<JsonConverter, DateTimeConverter>();
        coll.AddSingleton<JsonConverter, SizeConverter>();
        coll.AddSingleton<JsonConverterFactory, OptionalConverterFactory>();
        coll.AddSingleton<JsonConverter, OptionalConverterFactory>();

        // Game Registry
        coll.AddSingleton<IGameRegistry, GameRegistry.GameRegistry>();
        coll.AddHostedService(s => (GameRegistry.GameRegistry)s.GetRequiredService<IGameRegistry>());
        coll.AddGameInstallMetadataModel();
        
        // File Store
        coll.AddAllSingleton<IFileStore, NxFileStore>();
        
        // Readonly stream source
        coll.AddSingleton<IReadOnlyStreamSource>(s => s.GetRequiredService<NxFileStore>());
        coll.AddSingleton<IReadOnlyStreamSource, GameFileStreamSource>();
        coll.AddSingleton<IStreamSourceDispatcher, StreamSourceDispatcher>();
        
        coll.AddAllSingleton<IToolManager, ToolManager>();

        // Disk State and Synchronizer
        coll.AddDiskStateEntryModel();
        coll.AddAllSingleton<ISynchronizerService, SynchronizerService>();

        coll.AddSingleton<ITypeFinder>(_ => new AssemblyTypeFinder(typeof(Services).Assembly));
        coll.AddAllSingleton<ISorter, Sorter>();
        
        // Diagnostics
        coll.AddAllSingleton<IDiagnosticManager, DiagnosticManager>();
        coll.AddSettings<DiagnosticSettings>();
        
        // GC
        coll.AddAllSingleton<IGarbageCollectorRunner, GarbageCollectorRunner>();
        
        
        coll.AddPersistedDbResourceModel();
        
        // Undo
        coll.AddSingleton<UndoService>();

        // Verbs
        coll.AddLoadoutManagementVerbs()
            .AddImportExportVerbs()
            .AddToolVerbs();

        return coll;
    }
}
