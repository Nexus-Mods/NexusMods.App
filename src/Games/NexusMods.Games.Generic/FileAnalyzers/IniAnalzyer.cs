using System.Runtime.CompilerServices;
using IniParser;
using IniParser.Model.Configuration;
using IniParser.Parser;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Abstractions.Ids;
using NexusMods.FileExtractor.FileSignatures;
using NexusMods.Games.Generic.Entities;

namespace NexusMods.Games.Generic.FileAnalyzers;


public class IniAnalzyer : IFileAnalyzer
{
    public FileAnalyzerId Id { get; } = FileAnalyzerId.New("904bca7b-fbd6-4350-b4e2-6fdbd034ec76", 1);
    public IEnumerable<FileType> FileTypes => new[] { FileType.INI };

    public static readonly IniParserConfiguration Config = new()
    {
        AllowDuplicateKeys = true,
        AllowDuplicateSections = true,
        AllowKeysWithoutSection = true,
        AllowCreateSectionsOnFly = true,
        CaseInsensitive = true,
        SkipInvalidLines = true,
    };

#pragma warning disable CS1998
    public async IAsyncEnumerable<IFileAnalysisData> AnalyzeAsync(FileAnalyzerInfo info, [EnumeratorCancellation] CancellationToken token = default)
#pragma warning restore CS1998
    {
        var data = new StreamIniDataParser(new IniDataParser(Config)).ReadData(new StreamReader(info.Stream));
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
