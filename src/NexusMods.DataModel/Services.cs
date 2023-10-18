using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.CLI;
using NexusMods.Common;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.ArchiveMetaData;
using NexusMods.DataModel.Diagnostics;
using NexusMods.DataModel.GlobalSettings;
using NexusMods.DataModel.Interprocess;
using NexusMods.DataModel.Interprocess.Jobs;
using NexusMods.DataModel.JsonConverters;
using NexusMods.DataModel.JsonConverters.ExpressionGenerator;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.Mods;
using NexusMods.DataModel.RateLimiting;
using NexusMods.DataModel.Sorting.Rules;
using NexusMods.DataModel.TriggerFilter;
using NexusMods.DataModel.Verbs;
using NexusMods.Paths;

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

        IDataModelSettings Settings(IServiceProvider provider)
        {
            return provider.GetRequiredService<IDataModelSettings>();
        }

        coll.AddSingleton<JsonConverter, AbsolutePathConverter>();
        coll.AddSingleton<JsonConverter, RelativePathConverter>();
        coll.AddSingleton<JsonConverter, GamePathConverter>();
        coll.AddSingleton<JsonConverter, DateTimeConverter>();
        coll.AddSingleton<JsonConverter, SizeConverter>();
        coll.AddSingleton<JsonConverter, GameInstallationConverter>();
        coll.AddSingleton<JsonConverter, EntityHashSetConverterFactory>();
        coll.AddSingleton(typeof(EntityHashSetConverter<>));
        coll.AddSingleton<JsonConverter, EntityDictionaryConverterFactory>();
        coll.AddSingleton(typeof(EntityDictionaryConverter<,>));
        coll.AddSingleton<JsonConverter, EntityLinkConverterFactory>();
        coll.AddSingleton(typeof(EntityLinkConverter<>));

        coll.AddSingleton<IDataStore, SqliteDataStore>();
        coll.AddAllSingleton<IArchiveManager, NxArchiveManager>();
        coll.AddAllSingleton<IResource, IResource<FileHashCache, Size>>(s =>
            new Resource<FileHashCache, Size>("File Hashing",
                Settings(s).MaxHashingJobs,
                Size.FromLong(Settings(s).MaxHashingThroughputBytesPerSecond)));


        coll.AddSingleton(typeof(IFingerprintCache<,>), typeof(DataStoreFingerprintCache<,>));

        coll.AddSingleton<LoadoutRegistry>();
        coll.AddSingleton<IDirectoryIndexer, DirectoryIndexer>();
        coll.AddSingleton<IDownloadRegistry, DownloadRegistry>();
        coll.AddSingleton<FileHashCache>();
        coll.AddSingleton<GlobalSettingsManager>();
        coll.AddSingleton<IArchiveInstaller, ArchiveInstaller>();
        coll.AddSingleton<IToolManager, ToolManager>();
        coll.AddSingleton<DiskStateRegistry>();

        coll.AddAllSingleton<IInterprocessJobManager, SqliteIPC>();
        coll.AddSingleton(typeof(IMessageConsumer<>),
            typeof(InterprocessConsumer<>));
        coll.AddSingleton(typeof(IMessageProducer<>),
            typeof(InterprocessProducer<>));

        coll.AddSingleton<ITypeFinder>(_ => new AssemblyTypeFinder(typeof(Services).Assembly));
        coll.AddSingleton<JsonConverter, AbstractClassConverterFactory<AModMetadata>>();
        coll.AddSingleton<JsonConverter, AbstractClassConverterFactory<Entity>>();
        coll.AddSingleton<JsonConverter, AbstractClassConverterFactory<IMetadata>>();
        coll.AddSingleton<JsonConverter, AbstractClassConverterFactory<ISortRule<Mod, ModId>>>();
        coll.AddSingleton<JsonConverter, AbstractClassConverterFactory<AArchiveMetaData>>();

        coll.AddSingleton(s =>
        {
            var opts = new JsonSerializerOptions();
            opts.Converters.Add(new JsonStringEnumConverter());
            foreach (var converter in s.GetServices<JsonConverter>())
                opts.Converters.Add(converter);
            return opts;
        });

        // Diagnostics
        coll.AddAllSingleton<IDiagnosticManager, DiagnosticManager>();
        coll.AddOptions<DiagnosticOptions>();

        // Verbs
        coll.AddVerb<GenerateGameFileHashes>();

        return coll;
    }
}
