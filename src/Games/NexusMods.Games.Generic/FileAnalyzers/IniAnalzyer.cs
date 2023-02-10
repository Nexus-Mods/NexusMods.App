using IniParser;
using IniParser.Parser;
using NexusMods.DataModel.Abstractions;
using NexusMods.FileExtractor.FileSignatures;
using NexusMods.Games.Generic.Entities;

namespace NexusMods.Games.Generic.FileAnalyzers;


public class IniAnalzyer : IFileAnalyzer
{
    public IEnumerable<FileType> FileTypes => new[] {FileType.INI};
    public async IAsyncEnumerable<IFileAnalysisData> AnalyzeAsync(Stream stream, CancellationToken token = default)
    {
        var data = new StreamIniDataParser(new IniDataParser()).ReadData(new StreamReader(stream));
        var sections = data.Sections.Select(s => s.SectionName).ToHashSet();
        var keys = data.Global.Select(k => k.KeyName)
            .Concat(data.Sections.SelectMany(d => d.Keys).Select(kv => kv.KeyName))
            .ToHashSet();
        yield return new IniAnalysisData
        {
            Sections = sections,
            Keys = keys
        };
    }
}