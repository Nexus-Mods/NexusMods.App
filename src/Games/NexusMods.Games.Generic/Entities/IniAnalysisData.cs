using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.JsonConverters;
using NexusMods.DataModel.Loadouts;

namespace NexusMods.Games.Generic.Entities;

[JsonName("Generic.FileAnalysisData")]
public class IniAnalysisData : IFileAnalysisData
{
    public required HashSet<string> Sections { get; init; }
    public required HashSet<string> Keys { get; init; }
}