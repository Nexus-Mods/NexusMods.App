using System.Text;
using DuckDB.NET.Data;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Hashing.xxHash3;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Sdk.FileStore;
#pragma warning disable DuckDBNET001

namespace NexusMods.DataModel.QueryFunctions;

public class LoadStringFile : IQueryFunction
{
    private readonly IServiceProvider _provider;

    public LoadStringFile(IServiceProvider provider)
    {
        _provider = provider;
    }
    
    public void Register(DuckDBConnection connection, IQueryEngine engine)
    {
        connection.RegisterScalarFunction<ulong, string>("load_from_filestore", (rdrs, writer, row) =>
        {
            try
            {
                var fileStore = _provider.GetRequiredService<IFileStore>();
                var hash = Hash.From((ulong)rdrs[0].GetValue(0));
                var file = fileStore.Load(hash).Result;
                writer.WriteValue(Encoding.UTF8.GetString(file), 0);
            }
            catch (Exception ex)
            {
                // Log the error or handle it as needed
                writer.WriteNull(0);
            }
        });
    }
}
