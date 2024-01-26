using NexusMods.Abstractions.Serialization.ExpressionGenerator;
using NexusMods.Networking.Downloaders.Tasks.State;

namespace NexusMods.Networking.Downloaders;

public class TypeFinder : ITypeFinder
{
    public IEnumerable<Type> DescendentsOf(Type type)
    {
        return AllTypes.Where(t => t.IsAssignableTo(type));
    }

    private IEnumerable<Type> AllTypes => new[]
    {
        typeof(DownloaderState),
        typeof(NxmDownloadState),
        typeof(HttpDownloadState)
    };
}
