using NexusMods.DataModel.JsonConverters;
using NexusMods.DataModel.Loadouts;
using NexusMods.Paths;

namespace NexusMods.Games.BethesdaGameStudios;

[JsonName("BethesdaGameStudios.AnalysisSortData")]
public class AnalysisSortData : IModFileMetadata
{
    public required RelativePath[] Masters { get; init; } = Array.Empty<RelativePath>();
}