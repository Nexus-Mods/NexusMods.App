using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Binary.Streams;
using Mutagen.Bethesda.Plugins.Meta;
using Mutagen.Bethesda.Plugins.Records;
using Mutagen.Bethesda.Skyrim;
using NexusMods.DataModel.Abstractions.Ids;
using NexusMods.FileExtractor.FileSignatures;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;
using Noggog;

namespace NexusMods.Games.BethesdaGameStudios;

[UsedImplicitly]
public class PluginAnalyzer
{
    private static readonly HashSet<Extension> ValidExtensions = new Extension[] {
        new (".esp"),
        new (".esm"),
        new (".esl"),
    }.ToHashSet();

    private readonly ILogger<PluginAnalyzer> _logger;

    public PluginAnalyzer(ILogger<PluginAnalyzer> logger)
    {
        _logger = logger;
    }

    public async Task<PluginAnalysisData?> AnalyzeAsync(RelativePath path, Stream stream, CancellationToken ct = default)
    {
        var extension = path.Extension;
        if (!ValidExtensions.Contains(extension))
            return null;

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

        var fileAnalysisData = Analyze(path, GameConstants.SkyrimLE, stream);
        if (fileAnalysisData is null)
        {
            stream.Position = 0;
            fileAnalysisData = Analyze(path, GameConstants.Oblivion, stream);
            if (fileAnalysisData is null)
                return null;
        }

        return fileAnalysisData;
    }

    private PluginAnalysisData? Analyze(RelativePath path, GameConstants targetGame, Stream stream)
    {
        try
        {
            using var readStream = new MutagenInterfaceReadStream(
                new BinaryReadStream(stream, dispose: false),
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
            _logger.LogError(e, "Exception while parsing {} ({})", path.FileName, path);
            return null;
        }
    }
}
