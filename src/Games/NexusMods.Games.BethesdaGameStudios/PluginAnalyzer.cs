using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Binary.Headers;
using Mutagen.Bethesda.Skyrim;
using NexusMods.DataModel.Abstractions;
using NexusMods.FileExtractor.FileSignatures;
using NexusMods.Paths;

namespace NexusMods.Games.BethesdaGameStudios;

public class PluginAnalyzer : IFileAnalyzer
{
    public IEnumerable<FileType> FileTypes => new[] { FileType.TES4 };
    public async IAsyncEnumerable<IFileAnalysisData> AnalyzeAsync(Stream stream, CancellationToken ct = default)
    {
        var overlay = SkyrimMod.CreateFromBinaryOverlay(stream, SkyrimRelease.SkyrimSE, new ModKey("Dummy", ModType.Plugin));

        var masters = overlay.ModHeader.MasterReferences.Select(x => x.Master.FileName.ToString());
        yield return new PluginAnalysisData()
        {
            IsLightMaster = overlay.ModHeader.Flags.HasFlag(SkyrimModHeader.HeaderFlag.LightMaster),
            Masters = masters.Select(m => m.ToRelativePath()).ToArray()
        };
    }
}