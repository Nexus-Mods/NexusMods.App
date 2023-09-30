namespace NexusMods.Games.Generic.FileAnalyzers;

public class IniAnalysisData
{
    public required HashSet<string> Sections { get; init; }
    public required HashSet<string> Keys { get; init; }
}
