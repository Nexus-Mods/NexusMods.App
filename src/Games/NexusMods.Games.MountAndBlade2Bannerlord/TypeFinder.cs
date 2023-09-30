using NexusMods.DataModel.JsonConverters.ExpressionGenerator;
using NexusMods.Games.MountAndBlade2Bannerlord.Models;
using NexusMods.Games.MountAndBlade2Bannerlord.Sorters;

namespace NexusMods.Games.MountAndBlade2Bannerlord;

public class TypeFinder : ITypeFinder
{
    public IEnumerable<Type> DescendentsOf(Type type)
    {
        return AllTypes.Where(t => t.IsAssignableTo(type));
    }

    private static IEnumerable<Type> AllTypes => new[]
    {
        typeof(ModuleInfoMetadata),
        typeof(OriginalPathMetadata),
        typeof(ModuleInfoSort),
    };
}
