using NexusMods.Abstractions.GameLocators;
using NexusMods.HyperDuck;
using NexusMods.HyperDuck.Adaptor;
using NexusMods.HyperDuck.Adaptor.Impls;
using NexusMods.Paths;

namespace NexusMods.Abstractions.Loadouts.MnemonicDB;

public class GamePathValueAdaptor3 : IRowAdaptor<GamePath>
{
    public static void Adapt(RowCursor cursor, ref GamePath value)
    {
        var cell1 = cursor.GetValue<ushort>(1);
        var cell2 = cursor.GetValue<StringElement>(2);
        value = new GamePath(LocationId.From(cell1), RelativePath.FromUnsanitizedInput(cell2.GetString()));
    }
}

public class GamePathValueAdaptorFactory : IValueAdaptorFactory
{
    public bool TryExtractType(DuckDbType taggedType, LogicalType logicalType, Type type, out Type[] subTypes, out int priority)
    {
        if (type == typeof(GamePath) && taggedType == DuckDbType.String)
    }

    public Type CreateType(Registry registry, DuckDbType taggedType, LogicalType logicalType, Type resultTypes, Type[] subTypes, Type[] subAdaptors)
    {
        throw new NotImplementedException();
    }
}
