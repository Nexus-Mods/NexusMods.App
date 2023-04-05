using System.Runtime.CompilerServices;
using IniParser;
using IniParser.Parser;
using NexusMods.DataModel.Abstractions;
using NexusMods.FileExtractor.FileSignatures;
using NexusMods.Games.Generic.Entities;
using NexusMods.Paths;

namespace NexusMods.Games.Generic.FileAnalyzers;


public class IniAnalzyer : IFileAnalyzer
{
    public IEnumerable<FileType> FileTypes => new[] { FileType.INI };

#pragma warning disable CS1998
    public async IAsyncEnumerable<IFileAnalysisData> AnalyzeAsync(FileAnalyzerInfo info, [EnumeratorCancellation] CancellationToken token = default)
#pragma warning restore CS1998
    {
        var data = new StreamIniDataParser(new IniDataParser()).ReadData(new StreamReader(info.Stream));
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
