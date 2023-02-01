using NexusMods.DataModel.JsonConverters.ExpressionGenerator;
using NexusMods.Games.Generic.Entities;

namespace NexusMods.Games.Generic;

public class TypeFinder : ITypeFinder
{
    public IEnumerable<Type> DescendentsOf(Type type)
    {
        return AllTypes.Where(t => t.IsAssignableTo(type));
    }

    private IEnumerable<Type> AllTypes => new[]
    {
        typeof(IniAnalysisData),
    };
}