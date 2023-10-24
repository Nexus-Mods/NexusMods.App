using NexusMods.App.UI.Controls;
using NexusMods.App.UI.RightContent.LoadoutGrid;
using NexusMods.DataModel.JsonConverters.ExpressionGenerator;

namespace NexusMods.App.UI;

internal class TypeFinder : ITypeFinder
{
    public IEnumerable<Type> DescendentsOf(Type type)
    {
        return AllTypes.Where(t => t.IsAssignableTo(type));
    }

    private static IEnumerable<Type> AllTypes => new[]
    {
        typeof(DummyPageParameter),
        typeof(LoadoutGridParameter)
    };
}
