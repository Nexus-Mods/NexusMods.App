using System.Runtime.CompilerServices;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Abstractions.Ids;
using NexusMods.FileExtractor.FileSignatures;
using NexusMods.Games.DarkestDungeon.Models;

namespace NexusMods.Games.DarkestDungeon.Analyzers;

/// <summary>
/// <see cref="IFileAnalyzer"/> implementation for native Darkest Dungeon mods that have a
/// <c>project.xml</c> file. The file format is described in the game support document and
/// deserialized to <see cref="ModProject"/>.
/// </summary>
public class ProjectAnalyzer : IFileAnalyzer
{
    public FileAnalyzerId Id => FileAnalyzerId.New("3152ea61-a5a1-4d89-a780-25ebbab3da3f", 2);
    public IEnumerable<FileType> FileTypes => new[] { FileType.XML };

    public async IAsyncEnumerable<IFileAnalysisData> AnalyzeAsync(
        FileAnalyzerInfo info,
        [EnumeratorCancellation] CancellationToken token = default)
    {
        await Task.Yield();

        var res = Analyze(info);
        if (res is null) yield break;
        yield return res;
    }

    private static ModProject? Analyze(FileAnalyzerInfo info)
    {
        if (!info.FileName.Equals("project.xml", StringComparison.OrdinalIgnoreCase))
            return null;

        using var reader = XmlReader.Create(info.Stream, new XmlReaderSettings
        {
            IgnoreComments = true,
            IgnoreWhitespace = true,
            ValidationFlags = XmlSchemaValidationFlags.AllowXmlAttributes
        });

        var obj = new XmlSerializer(typeof(ModProject)).Deserialize(reader);
        return obj as ModProject;
    }
}
