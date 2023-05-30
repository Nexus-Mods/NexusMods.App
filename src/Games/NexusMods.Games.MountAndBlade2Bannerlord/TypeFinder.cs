using NexusMods.DataModel.JsonConverters.ExpressionGenerator;
using NexusMods.Games.MountAndBlade2Bannerlord.Analyzers;
using NexusMods.Games.MountAndBlade2Bannerlord.Loadouts;

namespace NexusMods.Games.MountAndBlade2Bannerlord;

public class TypeFinder : ITypeFinder
{
    private static IEnumerable<Type> AllTypes => new[]
    {
        typeof(MountAndBlade2BannerlordModuleInfo),
        typeof(ModuleIdMetadata),
        typeof(OriginalPathMetadata)
    };

    public IEnumerable<Type> DescendentsOf(Type type)
    {
        return AllTypes.Where(t => t.IsAssignableTo(type));
    }
}
