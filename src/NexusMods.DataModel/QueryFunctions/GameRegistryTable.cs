using System.Collections;
using System.Diagnostics.CodeAnalysis;
using DuckDB.NET.Data;
using DuckDB.NET.Data.DataChunk.Writer;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts;
using NexusMods.MnemonicDB.Abstractions;
#pragma warning disable DuckDBNET001

namespace NexusMods.DataModel.QueryFunctions;

public class GameRegistryTable : IQueryFunction
{
    private readonly IServiceProvider _provider;

    public GameRegistryTable(IServiceProvider provider)
    {
        _provider = provider;
    }
    
    public void Register(DuckDBConnection connection, IQueryEngine engine)
    {
        connection.RegisterTableFunction<int>("installed_games", args =>
        {
            var columns = new List<ColumnInfo>
            {
                new("Id", typeof(ulong)),
                new("Name", typeof(string)),
                new("Store", typeof(string)),
                new("LocatorIds", typeof(string)),
                new("Path", typeof(string)),
                new("LastScannedDiskStateTransactionId", typeof(ulong)),
                new("LastSyncedLoadoutId", typeof(ulong)),
                new("LastSyncedLoadoutTransactionId", typeof(ulong)),
            };

            var registry = _provider.GetRequiredService<IGameRegistry>();
            var db = engine.DefaultConnection().Db;
            return new TableFunction(columns, registry.Installations.Select(itm =>
            {
                var gameMetadata = GameInstallMetadata.Load(db, itm.Key);
                return (itm.Value, gameMetadata);
            }));
        }, MapRow);
    }

    private void MapRow(object? arg1, IDuckDBDataWriter[] writers, ulong row)
    {
        var (game, install) = ((GameInstallation, GameInstallMetadata.ReadOnly))arg1!;
        
        writers[0].WriteValue(game.GameMetadataId.Value, row);
        writers[1].WriteValue(game.Game.Name, row);
        writers[2].WriteValue(game.Store.Value, row);
        writers[3].WriteValue(string.Join(',', game.LocatorResultMetadata!.ToLocatorIds().Select(id => id.ToString())), row);
        writers[4].WriteValue(game.LocationsRegister.GetResolvedPath(LocationId.Game).ToString(), row);
        
        if (GameInstallMetadata.LastScannedDiskStateTransactionId.TryGetValue(install, out var id)) 
            writers[5].WriteValue(id.Value, row);
        else 
            writers[5].WriteNull(row);
        
        if (GameInstallMetadata.LastSyncedLoadoutId.TryGetValue(install, out id)) 
            writers[6].WriteValue(id.Value, row);
        else 
            writers[6].WriteNull(row);
        
        if (GameInstallMetadata.LastSyncedLoadoutTransactionId.TryGetValue(install, out id)) 
            writers[7].WriteValue(id.Value, row);
        else 
            writers[7].WriteNull(row);
    }
}
