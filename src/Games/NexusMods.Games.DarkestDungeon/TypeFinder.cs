using NexusMods.Abstractions.Serialization.ExpressionGenerator;
using NexusMods.Games.DarkestDungeon.Models;

namespace NexusMods.Games.DarkestDungeon;

public class TypeFinder : ITypeFinder
{
    public IEnumerable<Type> DescendentsOf(Type type)
    {
        return AllTypes.Where(t => t.IsAssignableTo(type));
    }

    private static IEnumerable<Type> AllTypes => new[]
    {
        typeof(ModProject),
    };
}
