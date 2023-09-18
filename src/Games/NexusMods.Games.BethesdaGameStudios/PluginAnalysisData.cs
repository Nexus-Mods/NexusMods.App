using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.JsonConverters;
using NexusMods.Paths;

namespace NexusMods.Games.BethesdaGameStudios;

[JsonName("BethesdaGameStudios.FileAnalysisData")]
public class PluginAnalysisData
{
    public required RelativePath[] Masters { get; init; }
    public bool IsLightMaster { get; init; }
}
