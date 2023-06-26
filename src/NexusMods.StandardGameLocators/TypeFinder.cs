using NexusMods.DataModel.JsonConverters.ExpressionGenerator;

namespace NexusMods.StandardGameLocators;

internal class TypeFinder : ITypeFinder
{
    /// <inheritdoc />
    public IEnumerable<Type> DescendentsOf(Type type)
    {
        return AllTypes.Where(t => t.IsAssignableTo(type));
    }

    private IEnumerable<Type> AllTypes => new[]
    {
        typeof(ManuallyAddedGame)
    };
}
