using NexusMods.Abstractions.Serialization.ExpressionGenerator;
using NexusMods.App.UI.Controls;
using NexusMods.App.UI.RightContent.LoadoutGrid;

namespace NexusMods.App.UI;

internal class TypeFinder : ITypeFinder
{
    public IEnumerable<Type> DescendentsOf(Type type)
    {
        return AllTypes.Where(t => t.IsAssignableTo(type));
    }

    private static IEnumerable<Type> AllTypes => new[]
    {
        typeof(DummyPageContext),
        typeof(LoadoutGridContext)
    };
}
