using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.JsonConverters;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.RateLimiting;
using NexusMods.DataModel.Sorting;
using NexusMods.Hashing.xxHash64;
using NexusMods.Interfaces;
using NexusMods.Paths;

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
        coll.AddSingleton<JsonConverter, HashConverter>();
        coll.AddSingleton<JsonConverter, GameInstallationConverter>();
        coll.AddSingleton<JsonConverter, EntityHashSetConverterFactory>();
        coll.AddSingleton(typeof(EntityHashSetConverter<>));
        coll.AddSingleton<JsonConverter, EntityDictionaryConverterFactory>();
        coll.AddSingleton(typeof(EntityDictionaryConverter<,>));
        coll.AddSingleton<JsonConverter, EntityLinkConverterFactory>();
        coll.AddSingleton(typeof(EntityLinkConverter<>));

        coll.AddSingleton<JsonConverter, ISortRuleConverterFactory>();
        coll.AddSingleton(typeof(ISortRuleConverter<,>));
        
        coll.AddSingleton<IDataStore>(s => new LMDBDataStore(baseFolder.Value.Join("DataModel_LMDB"), s));
        coll.AddSingleton(s => new ArchiveManager(s.GetRequiredService<ILogger<ArchiveManager>>(),
            new []{baseFolder.Value.Join("Archives")},
            s.GetRequiredService<IDataStore>(),
            s.GetRequiredService<FileExtractor.FileExtractor>(),
            s.GetRequiredService<ArchiveContentsCache>()));
        coll.AddAllSingleton<IResource, IResource<FileHashCache, Size>>(_ => new Resource<FileHashCache, Size>("File Hashing", Environment.ProcessorCount, Size.Zero));
        coll.AddAllSingleton<IResource, IResource<LoadoutManager, Size>>(_ => new Resource<LoadoutManager, Size>("Load Order Management", Environment.ProcessorCount, Size.Zero));
        coll.AddSingleton<LoadoutManager>();
        coll.AddSingleton<FileHashCache>();
        coll.AddSingleton<ArchiveContentsCache>();
        
        NexusMods_DataModel_Abstractions_EntityConverter.ConfigureServices(coll);
        
        coll.AddSingleton(s =>
        {
            var opts = new JsonSerializerOptions();
            foreach (var converter in s.GetServices<JsonConverter>())
                opts.Converters.Add(converter);
            return opts;
        });
        return coll;
    }
    
}