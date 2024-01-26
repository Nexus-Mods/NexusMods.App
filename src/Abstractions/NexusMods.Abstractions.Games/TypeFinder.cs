using NexusMods.Abstractions.Games.ArchiveMetadata;
using NexusMods.Abstractions.Games.Downloads;
using NexusMods.Abstractions.Games.Loadouts;
using NexusMods.Abstractions.Serialization.ExpressionGenerator;

namespace NexusMods.Abstractions.Games;

/// <summary>
///     Info for the Nexus serializer.
/// </summary>
public class TypeFinder : ITypeFinder
{
    public IEnumerable<Type> DescendentsOf(Type type)
    {
        return AllTypes.Where(t => t.IsAssignableTo(type));
    }

    private static IEnumerable<Type> AllTypes => new[]
    {
        typeof(AArchiveMetaData),
        typeof(FilePathMetadata),
        typeof(GameArchiveMetadata),
        typeof(NexusModsArchiveMetadata),
        typeof(DownloadAnalysis),
        typeof(Loadout),
    };
}
