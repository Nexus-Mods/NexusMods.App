using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Synchronizers;
using NexusMods.HyperDuck;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;

namespace NexusMods.DataModel.Synchronizer.DbFunctions;

/// <summary>
/// A Table function that lists all intrinsic files for all loadouts. 
/// </summary>
public class IntrinsicFiles : ATableFunction
{
    private readonly Lazy<IConnection> _connection;

    public IntrinsicFiles(IServiceProvider serviceProvider)
    {
        _connection = new Lazy<IConnection>(serviceProvider.GetRequiredService<IConnection>);
    }
    
    protected override void Setup(RegistrationInfo info)
    {
        info.SetName("intrinsic_files");
        info.AddNamedParameter<ulong>("Db");
    }

    protected override void Execute(FunctionInfo functionInfo)
    {
        var executeData = functionInfo.GetInitInfo<ExecuteData>();
        var chunk = functionInfo.Chunk;

        var loadoutIds = chunk[0].GetData<EntityId>();
        var path = chunk[1];
        var locationIds = path.GetStructChild(0).GetData<LocationId>();
        var pathStrings = path.GetStructChild(1);
        
        var row = 0;
        while (!executeData.Finished)
        {
            if (!executeData.Data.MoveNext())
            {
                executeData.Finished = true;
                break;
            }
            var current = executeData.Data.Current;
            loadoutIds[row] = current.Item1;
            locationIds[row] = current.Item2.LocationId;
            
            pathStrings.WriteUtf16((ulong)row, current.Item2.Path);
            row++;
        }

        chunk.Size = (ulong)row;
        
    }

    protected override object? Init(InitInfo initInfo, InitData initData)
    {
        var bindData = initInfo.GetBindData<BindData>();
        var db = bindData.Db;

        var data = from loadout in Loadout.All(db)
            let game = ((IGame)loadout.LocatableGame)
            let synchronizer = (ALoadoutSynchronizer)game.Synchronizer
            from intrinsic in synchronizer.IntrinsicFiles(loadout)
            select (loadout.LoadoutId.Value, intrinsic.Key);
        
        return new ExecuteData()
        {
            Data = data.GetEnumerator()
        };
    }

    protected override void Bind(BindInfo info)
    {
        var db = _connection.Value.Db;
        using var param = info.GetParameter("Db");
        if (!param.IsNull)
        {
            var asOf = param.GetUInt64() >> 16;
            var asOfTxId = TxId.From(PartitionId.Transactions.MakeEntityId(asOf).Value);
            db = _connection.Value.AsOf(asOfTxId);
        }
        
        info.SetBindInfo(new BindData()
        {
            Db = db,
        });
            
        
        info.AddColumn<ulong>("Loadout");
        info.AddColumn("Path", ValueTag.Tuple2_UShort_Utf8I.DuckDbType(typeof(string)));
    }

    record ExecuteData
    {
        public required IEnumerator<(EntityId, GamePath)> Data { get; init; }
        public bool Finished { get; set; } = false;
    }

    record BindData
    {
        public required IDb Db { get; init; }
    }
}
