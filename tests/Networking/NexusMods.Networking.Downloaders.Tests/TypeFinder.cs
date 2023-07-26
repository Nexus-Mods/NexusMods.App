using NexusMods.DataModel.JsonConverters.ExpressionGenerator;
using NexusMods.Networking.Downloaders.Interfaces;
using NexusMods.Networking.Downloaders.Tasks.State;
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
