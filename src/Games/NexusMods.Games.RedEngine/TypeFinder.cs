using NexusMods.DataModel.JsonConverters.ExpressionGenerator;
using NexusMods.Games.RedEngine.FileAnalyzers;

namespace NexusMods.Games.RedEngine;

public class TypeFinder : ITypeFinder
{
    public IEnumerable<Type> DescendentsOf(Type type)
    {
        return AllTypes.Where(t => t.IsAssignableTo(type));
    }

    private IEnumerable<Type> AllTypes => new[]
    {
        typeof(RedModInfo)
    };
}
