using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Common;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Interprocess;
using NexusMods.DataModel.Interprocess.Messages;
using NexusMods.DataModel.JsonConverters;
using NexusMods.DataModel.JsonConverters.ExpressionGenerator;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.RateLimiting;
using NexusMods.DataModel.Sorting.Rules;
using NexusMods.Paths;
using NexusMods.Paths.Utilities;

namespace NexusMods.DataModel;

public static class Services
{
    public static IServiceCollection AddDataModel(this IServiceCollection coll, AbsolutePath? baseFolder = null)
    {
        baseFolder ??= KnownFolders.EntryFolder;
        baseFolder.Value.CreateDirectory();

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

        coll.AddSingleton<IDataStore>(s => new SqliteDataStore(
            s.GetRequiredService<ILogger<SqliteDataStore>>(),
            baseFolder.Value.CombineUnchecked("DataModel.sqlite"), s,
            s.GetRequiredService<IMessageProducer<RootChange>>(),
            s.GetRequiredService<IMessageConsumer<RootChange>>(),
            s.GetRequiredService<IMessageProducer<IdPut>>(),
            s.GetRequiredService<IMessageConsumer<IdPut>>()));
        coll.AddSingleton(s => new ArchiveManager(s.GetRequiredService<ILogger<ArchiveManager>>(),
            new[] { baseFolder.Value.CombineUnchecked("Archives") },
            s.GetRequiredService<IDataStore>(),
            s.GetRequiredService<FileExtractor.FileExtractor>(),
            s.GetRequiredService<FileContentsCache>()));
        coll.AddAllSingleton<IResource, IResource<FileHashCache, Size>>(_ => new Resource<FileHashCache, Size>("File Hashing", Environment.ProcessorCount, Size.Zero));
        coll.AddAllSingleton<IResource, IResource<LoadoutManager, Size>>(_ => new Resource<LoadoutManager, Size>("Load Order Management", Environment.ProcessorCount, Size.Zero));
        coll.AddSingleton<LoadoutManager>();
        coll.AddSingleton<FileHashCache>();
        coll.AddSingleton<FileContentsCache>();

        coll.AddSingleton(s => new SqliteIPC(
            s.GetRequiredService<ILogger<SqliteIPC>>(),
            baseFolder.Value.CombineUnchecked("DataModel_IPC.sqlite"),
            baseFolder.Value.CombineUnchecked("DataModel_IPC.mmap")));
        coll.AddSingleton(typeof(IMessageConsumer<>), typeof(InterprocessConsumer<>));
        coll.AddSingleton(typeof(IMessageProducer<>), typeof(InterprocessProducer<>));

        coll.AddSingleton<ITypeFinder>(_ => new AssemblyTypeFinder(typeof(Services).Assembly));
        coll.AddSingleton<JsonConverter, AbstractClassConverterFactory<Entity>>();
        coll.AddSingleton<JsonConverter, AbstractClassConverterFactory<ISortRule<Mod, ModId>>>();
        coll.AddSingleton<JsonConverter, AbstractClassConverterFactory<IModFileMetadata>>();
        coll.AddSingleton<JsonConverter, AbstractClassConverterFactory<IFileAnalysisData>>();

        coll.AddSingleton(s =>
        {
            var opts = new JsonSerializerOptions();
            opts.Converters.Add(new JsonStringEnumConverter());
            foreach (var converter in s.GetServices<JsonConverter>())
                opts.Converters.Add(converter);
            return opts;
        });
        return coll;
    }

}
