using System.Runtime.CompilerServices;
using System.Xml;
using Bannerlord.LauncherManager;
using Bannerlord.ModuleManager;
using Microsoft.Extensions.Logging;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Abstractions.Ids;
using NexusMods.FileExtractor.FileSignatures;

namespace NexusMods.Games.MountAndBlade2Bannerlord.Analyzers;

public class MountAndBlade2BannerlordAnalyzer : IFileAnalyzer
{
    public FileAnalyzerId Id { get; } = FileAnalyzerId.New("dce08909-ff0d-4b1b-9d2b-f2144563cf9f", 1);
    public IEnumerable<FileType> FileTypes { get; } = new[] { FileType.XML };

    private readonly ILogger<MountAndBlade2BannerlordAnalyzer> _logger;

    public MountAndBlade2BannerlordAnalyzer(ILogger<MountAndBlade2BannerlordAnalyzer> logger)
    {
        _logger = logger;
    }

    public async IAsyncEnumerable<IFileAnalysisData> AnalyzeAsync(FileAnalyzerInfo info, [EnumeratorCancellation] CancellationToken ct = default)
    {
        await Task.Yield();

        // Not sourced from an archive.
        if (info.RelativePath == null)
            yield break;

        // Check if file is "SubModule.xml"
        if (!info.FileName.Equals(Constants.SubModuleName))
            yield break;

        // If not from inside an archive, this is probably not a Bannerlord module.
        if (info.ParentArchive == null)
            yield break;

        // Now get the actual items out.
        // Determine if this is a supported Bannerlord module.
        ModuleInfoExtended data;

        try
        {
            var doc = new XmlDocument();
            doc.Load(info.Stream);
            data = ModuleInfoExtended.FromXml(doc);
        }
        catch (Exception e)
        {
            _logger.LogError("Failed to Parse Bannerlord Module: {EMessage}\\n{EStackTrace}", e.Message, e.StackTrace);
            yield break;
        }

        yield return new MountAndBlade2BannerlordModuleInfo
        {
            ModuleInfo = data
        };
    }
}