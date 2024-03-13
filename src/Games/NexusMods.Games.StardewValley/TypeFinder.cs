using NexusMods.Abstractions.Serialization.ExpressionGenerator;
using NexusMods.Games.StardewValley.Models;
using NexusMods.Games.StardewValley.Sorters;

namespace NexusMods.Games.StardewValley;

public class TypeFinder : ITypeFinder
{
    public IEnumerable<Type> DescendentsOf(Type type)
    {
        return AllTypes.Where(t => t.IsAssignableTo(type));
    }

    private static IEnumerable<Type> AllTypes => new[]
    {
        typeof(SMAPIMarker),
        typeof(SMAPIModMarker),
        typeof(SMAPIManifestMetadata),
        typeof(SMAPISorter),
    };
}
