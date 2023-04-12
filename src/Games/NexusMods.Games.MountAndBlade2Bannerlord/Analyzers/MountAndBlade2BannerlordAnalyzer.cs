using System.Runtime.CompilerServices;
using System.Xml;
using Bannerlord.LauncherManager;
using Bannerlord.ModuleManager;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.JsonConverters;
using NexusMods.FileExtractor.FileSignatures;
using NexusMods.Paths;

namespace NexusMods.Games.MountAndBlade2Bannerlord;

public class MountAndBlade2BannerlordAnalyzer : IFileAnalyzer
{
    private readonly ILogger<MountAndBlade2BannerlordAnalyzer> _logger;
    private readonly IFileSystem _fileSystem;

    public MountAndBlade2BannerlordAnalyzer(ILogger<MountAndBlade2BannerlordAnalyzer> logger, IFileSystem fileSystem)
    {
        _logger = logger;
        _fileSystem = fileSystem;
    }

    public IEnumerable<FileType> FileTypes { get; } = new[] { FileType.XML };

    public async IAsyncEnumerable<IFileAnalysisData> AnalyzeAsync(FileAnalyzerInfo info, [EnumeratorCancellation] CancellationToken ct = default)
    {
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
        ModuleInfoExtended? data;

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

        if (data is null)
        {
            _logger.LogError("Failed to Parse SubModule.xml of the Bannerlord Module: {File}", info.RelativePath.Value.ToString());
            yield break;
        }

        yield return new MountAndBlade2BannerlordModuleInfo
        {
            ModuleInfo = data
        };
    }
}

[PublicAPI]
[JsonName("NexusMods.Games.MountAndBlade2Bannerlord.MountAndBlade2BannerlordModuleInfo")]
public record MountAndBlade2BannerlordModuleInfo : IFileAnalysisData
{
    public required ModuleInfoExtended ModuleInfo { get; init; }
}
