using IniParser;
using IniParser.Model.Configuration;
using IniParser.Parser;
using NexusMods.Abstractions.IO;

namespace NexusMods.Games.Generic.FileAnalyzers;


public class IniAnalzyer
{
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
