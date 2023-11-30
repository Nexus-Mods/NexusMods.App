using NexusMods.DataModel.JsonConverters.ExpressionGenerator;
using NexusMods.Games.BladeAndSorcery.Models;

namespace NexusMods.Games.BladeAndSorcery;

public class TypeFinder : ITypeFinder
{
    public IEnumerable<Type> DescendentsOf(Type type)
    {
        return AllTypes.Where(t => t.IsAssignableTo(type));
    }

    private static IEnumerable<Type> AllTypes => new[]
    {
        typeof(ModManifest)
    };
}
