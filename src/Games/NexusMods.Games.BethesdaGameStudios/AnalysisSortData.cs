using System.Text.Json.Serialization;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.JsonConverters;
using NexusMods.DataModel.Loadouts;
using NexusMods.Paths;

namespace NexusMods.Games.BethesdaGameStudios;

public class AnalysisSortData : IModFileMetadata
{
    public required RelativePath[] Masters { get; init; } = Array.Empty<RelativePath>();
}