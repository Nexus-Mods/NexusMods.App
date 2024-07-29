using NexusMods.Abstractions.Loadouts.Files;
using NexusMods.Abstractions.Serialization.ExpressionGenerator;

namespace NexusMods.Abstractions.Installers;

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
        typeof(StoredFile),
    };
}
