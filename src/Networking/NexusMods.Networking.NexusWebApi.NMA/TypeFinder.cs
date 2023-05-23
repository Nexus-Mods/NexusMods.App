using NexusMods.DataModel.JsonConverters.ExpressionGenerator;

namespace NexusMods.Networking.NexusWebApi.NMA;

/// <summary>
/// find types for use in the data store
/// </summary>
public class TypeFinder : ITypeFinder
{
    /// <inheritdoc/>
    public IEnumerable<Type> DescendentsOf(Type type)
    {
        return AllTypes.Where(t => t.IsAssignableTo(type));
    }

    private IEnumerable<Type> AllTypes => new[]
    {
        typeof(JWTTokenEntity)
    };
}
