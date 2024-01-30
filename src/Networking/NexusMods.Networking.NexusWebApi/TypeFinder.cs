using NexusMods.Abstractions.NexusWebApi;
using NexusMods.Abstractions.Serialization.ExpressionGenerator;
using NexusMods.Networking.NexusWebApi.Auth;

namespace NexusMods.Networking.NexusWebApi;

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
        typeof(JWTTokenEntity),
        typeof(NexusModsArchiveMetadata)
    };
}
