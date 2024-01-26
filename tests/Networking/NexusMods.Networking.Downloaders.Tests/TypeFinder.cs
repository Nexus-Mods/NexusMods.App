using NexusMods.Abstractions.Serialization.ExpressionGenerator;
using NexusMods.Networking.Downloaders.Tests.Serialization;

namespace NexusMods.Networking.Downloaders.Tests;

public class TypeFinder : ITypeFinder
{
    public IEnumerable<Type> DescendentsOf(Type type)
    {
        return AllTypes.Where(t => t.IsAssignableTo(type));
    }

    private IEnumerable<Type> AllTypes => new[]
    {
        typeof(MockDownloaderState)
    };
}
