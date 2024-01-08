using IniParser;
using IniParser.Model.Configuration;
using IniParser.Parser;
using NexusMods.Common;
using NexusMods.DataModel.Abstractions.Ids;
using NexusMods.FileExtractor.FileSignatures;

namespace NexusMods.Games.Generic.FileAnalyzers;


public class IniAnalzyer
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

    public static async Task<IniAnalysisData> AnalyzeAsync(IStreamFactory info)
    {
        await using var os = await info.GetStreamAsync();
        var data = new StreamIniDataParser(new IniDataParser(Config)).ReadData(new StreamReader(os));
        var sections = data.Sections.Select(s => s.SectionName).ToHashSet();
        var keys = data.Global.Select(k => k.KeyName)
            .Concat(data.Sections.SelectMany(d => d.Keys).Select(kv => kv.KeyName))
            .ToHashSet();
        return new IniAnalysisData
        {
            Sections = sections,
            Keys = keys
        };
    }
}
