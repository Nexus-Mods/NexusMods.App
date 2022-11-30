using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.JsonConverters;
using NexusMods.DataModel.ModLists;
using NexusMods.Paths;

namespace NexusMods.DataModel;

public static class Services
{
    public static IServiceCollection AddDataModel(this IServiceCollection coll)
    {
        coll.AddSingleton<JsonConverter, RelativePathConverter>();
        coll.AddSingleton<JsonConverter, GamePathConverter>();
        coll.AddSingleton<IDataStore>(s => new RocksDbDatastore(KnownFolders.CurrentDirectory.Combine("DataModel"),
            s.GetRequiredService<DataModelJsonContext>()));
        coll.AddSingleton<ModListManager>();
        coll.AddSingleton(s =>
        {
            var opts = new JsonSerializerOptions();
            foreach (var converter in s.GetServices<JsonConverter>())
                opts.Converters.Add(converter);
            return new DataModelJsonContext(opts);
        });
        return coll;
    }
    
}