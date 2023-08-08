using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Binary.Streams;
using Mutagen.Bethesda.Plugins.Meta;
using Mutagen.Bethesda.Plugins.Records;
using Mutagen.Bethesda.Skyrim;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Abstractions.Ids;
using NexusMods.FileExtractor.FileSignatures;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;
using Noggog;

namespace NexusMods.Games.BethesdaGameStudios;

[PublicAPI]
public class PluginAnalyzer : IFileAnalyzer
{
    public FileAnalyzerId Id { get; } = FileAnalyzerId.New("9c673a4f-064f-4b1e-83e3-4bf0454575cd", 1);

    public IEnumerable<FileType> FileTypes => new[] { FileType.TES4 };

    private static readonly Extension[] ValidExtensions = {
        new(".esp"),
        new(".esm"),
        new(".esl"),
    };

    private readonly ILogger<PluginAnalyzer> _logger;

    public PluginAnalyzer(ILogger<PluginAnalyzer> logger)
    {
        _logger = logger;
    }

#pragma warning disable CS1998
    public async IAsyncEnumerable<IFileAnalysisData> AnalyzeAsync(FileAnalyzerInfo info, [EnumeratorCancellation] CancellationToken ct = default)
#pragma warning restore CS1998
    {
        var extension = info.FileName.ToRelativePath().Extension;
        if (ValidExtensions[0] != extension && ValidExtensions[1] != extension && ValidExtensions[2] != extension) yield break;

        // NOTE(erri120): The GameConstant specifies the header length.
        // - Oblivion: 20 bytes
        // - Skyrim (every release): 24 bytes
        // - Fallout 4: 24 bytes
        // We're only looking for the "Light Master" flag and all Master References.
        // The Flags are at 8+4 bytes, so we can use the smallest header length for those.
        // However, the Master References are just Subrecords that appear directly after
        // the header.
        // The current solution just tries different GameConstants, which isn't ideal and
        // should be replaced with an identification step that finds the correct GameConstant.

        var startPos = info.Stream.Position;
        var fileAnalysisData = Analyze(GameConstants.SkyrimLE, info);
        if (fileAnalysisData is null)
        {
            info.Stream.Position = startPos;
            fileAnalysisData = Analyze(GameConstants.Oblivion, info);
            if (fileAnalysisData is null) yield break;
        }

        yield return fileAnalysisData;
    }

    private IFileAnalysisData? Analyze(GameConstants targetGame, FileAnalyzerInfo info)
    {
        try
        {
            using var readStream = new MutagenInterfaceReadStream(
                new BinaryReadStream(info.Stream, dispose: false),
                new ParsingBundle(targetGame, masterReferences: null!)
                {
                    ModKey = ModKey.Null
                }
            );

            var frame = new MutagenFrame(readStream);
            var header = readStream.ReadModHeaderFrame(readSafe: true);

            var isLightMaster = (header.Flags & (int)SkyrimModHeader.HeaderFlag.LightMaster) ==
                                (int)SkyrimModHeader.HeaderFlag.LightMaster;

            var masters = header
                .Masters()
                .Select(masterPinFrame =>
                {
                    readStream.Position = masterPinFrame.Location;
                    return MasterReference.CreateFromBinary(frame);
                })
                .Select(masterReference => masterReference.Master.FileName.ToString().ToRelativePath())
                .ToArray();

            return new PluginAnalysisData
            {
                IsLightMaster = isLightMaster,
                Masters = masters
            };
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Exception while parsing {} ({})", info.FileName, info.RelativePath);
            return null;
        }
    }
}
