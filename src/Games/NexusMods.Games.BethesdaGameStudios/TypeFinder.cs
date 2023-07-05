using NexusMods.DataModel.JsonConverters.ExpressionGenerator;

namespace NexusMods.Games.BethesdaGameStudios;

public class TypeFinder : ITypeFinder
{
    public IEnumerable<Type> DescendentsOf(Type type)
    {
        return AllTypes.Where(t => t.IsAssignableTo(type));
    }

    private static IEnumerable<Type> AllTypes => new[]
    {
        typeof(PluginAnalysisData),
        typeof(PluginFile),
    };
}
