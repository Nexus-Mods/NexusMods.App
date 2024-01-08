using NexusMods.DataModel.JsonConverters;
using NexusMods.Paths;

namespace NexusMods.Games.BethesdaGameStudios;

[JsonName("BethesdaGameStudios.FileAnalysisData")]
public class PluginAnalysisData
{
    /// <summary>
    /// The masters of this plugin, this is always Skyrim.esm and often the DLCs
    /// </summary>
    public required RelativePath[] Masters { get; init; }

    /// <summary>
    /// True if this is a light master, this will be true for some ESP files
    /// </summary>
    public bool IsLightMaster { get; init; }

    /// <summary>
    /// The name of this plugin, such as "Skyrim.esm"
    /// </summary>
    public required RelativePath FileName { get; init; }
}
