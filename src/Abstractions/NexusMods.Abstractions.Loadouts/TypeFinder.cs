using NexusMods.Abstractions.DataModel.Entities.Sorting;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.Abstractions.Serialization.ExpressionGenerator;

namespace NexusMods.Abstractions.Loadouts;

/// <summary>
///     Info for the Nexus serializer.
/// </summary>
public class TypeFinder : ITypeFinder
{
    /// <inheritdoc />
    public IEnumerable<Type> DescendentsOf(Type type)
    {
        return AllTypes.Where(t => t.IsAssignableTo(type));
    }

    private static IEnumerable<Type> AllTypes => new[]
    {
        typeof(Loadout),
        typeof(Mod),
        typeof(After<Mod,ModId>),
        typeof(Before<Mod,ModId>),
        typeof(First<Mod,ModId>),
        typeof(ISortRule<Mod,ModId>),
    };
}
