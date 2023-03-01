using NexusMods.DataModel.JsonConverters.ExpressionGenerator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        typeof(JWTTokenEntity)
    };
}
