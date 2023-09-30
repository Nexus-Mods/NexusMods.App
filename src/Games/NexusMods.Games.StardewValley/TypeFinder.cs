using NexusMods.DataModel.JsonConverters.ExpressionGenerator;
using NexusMods.Games.StardewValley.Models;

namespace NexusMods.Games.StardewValley;

public class TypeFinder : ITypeFinder
{
    public IEnumerable<Type> DescendentsOf(Type type)
    {
        return AllTypes.Where(t => t.IsAssignableTo(type));
    }

    private static IEnumerable<Type> AllTypes => new[]
    {
        typeof(SMAPIManifest),
        typeof(SMAPIManifestDependency),
        typeof(SMAPIVersion)
    };
}
