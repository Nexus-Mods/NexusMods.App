using System.Collections.Immutable;
using JetBrains.Annotations;
using NexusMods.DataModel.Abstractions;

namespace NexusMods.DataModel.Extensions;

/// <summary>
/// Extensions for <see cref="IMetadata"/>.
/// </summary>
[PublicAPI]
public static class MetadataExtensions
{
    /// <summary>
    /// Casts the elements of <paramref name="list"/> to <see cref="IMetadata"/>.
    /// </summary>
    /// <param name="list"></param>
    /// <returns></returns>
    public static ImmutableList<IMetadata> AsMetadata(this ImmutableList<IFileAnalysisData> list)
    {
        return list.Cast<IMetadata>().ToImmutableList();
    }
}
