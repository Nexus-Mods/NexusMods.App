using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.App.Settings;
using NexusMods.Abstractions.Diagnostics;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Games.Loadouts;
using NexusMods.Abstractions.Games.Loadouts.Sorting;
using NexusMods.Abstractions.IO;
using NexusMods.Abstractions.Messaging;
using NexusMods.Abstractions.Serialization;
using NexusMods.Abstractions.Serialization.ExpressionGenerator;
using NexusMods.DataModel.CommandLine.Verbs;
using NexusMods.DataModel.Diagnostics;
using NexusMods.DataModel.GlobalSettings;
using NexusMods.DataModel.JsonConverters;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Messaging;
using NexusMods.DataModel.Sorting;
using NexusMods.DataModel.TriggerFilter;
using NexusMods.Extensions.DependencyInjection;

namespace NexusMods.DataModel;

/// <summary/>
public static class Services
{
    /// <summary>
    /// Adds all services related to the <see cref="DataModel"/> to your dependency
    /// injection container.
    /// </summary>
    public static IServiceCollection AddDataModel(this IServiceCollection coll,
        IDataModelSettings? settings = null)
    {
        if (settings == null)
            coll.AddSingleton<IDataModelSettings, DataModelSettings>();
        else
            coll.AddSingleton(settings);

        coll.AddSingleton<MessageBus>();
        coll.AddSingleton(typeof(IMessageConsumer<>), typeof(MessageConsumer<>));
        coll.AddSingleton(typeof(IMessageProducer<>), typeof(MessageProducer<>));

        coll.AddSingleton<JsonConverter, AbsolutePathConverter>();
        coll.AddSingleton<JsonConverter, RelativePathConverter>();
        coll.AddSingleton<JsonConverter, GamePathConverter>();
        coll.AddSingleton<JsonConverter, DateTimeConverter>();
        coll.AddSingleton<JsonConverter, SizeConverter>();


        coll.AddAllSingleton<IDataStore, SqliteDataStore>();
        coll.AddAllSingleton<IFileStore, NxFileStore>();

        coll.AddSingleton(typeof(IFingerprintCache<,>), typeof(DataStoreFingerprintCache<,>));

        coll.AddDataModelSettings();
        coll.AddAllSingleton<ILoadoutRegistry, LoadoutRegistry>();
        coll.AddAllSingleton<IDirectoryIndexer, DirectoryIndexer>();
        coll.AddAllSingleton<IFileOriginRegistry, FileOriginRegistry>();
        coll.AddAllSingleton<IFileHashCache, FileHashCache>();
        coll.AddAllSingleton<IArchiveInstaller, ArchiveInstaller>();
        coll.AddAllSingleton<IToolManager, ToolManager>();
        coll.AddAllSingleton<IDiskStateRegistry, DiskStateRegistry>();

        coll.AddSingleton<ITypeFinder>(_ => new AssemblyTypeFinder(typeof(Services).Assembly));
        coll.AddAllSingleton<ISorter, Sorter>();

        // Diagnostics
        coll.AddAllSingleton<IDiagnosticManager, DiagnosticManager>();
        coll.AddOptions<DiagnosticOptions>();

        // Verbs
        coll.AddLoadoutManagementVerbs()
            .AddToolVerbs()
            .AddFileHashCacheVerbs()
            .AddArchiveVerbs();

        return coll;
    }
}
