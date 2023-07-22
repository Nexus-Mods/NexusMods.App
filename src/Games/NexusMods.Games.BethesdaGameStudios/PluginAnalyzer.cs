using System.Runtime.CompilerServices;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Abstractions.Ids;
using NexusMods.FileExtractor.FileSignatures;
using NexusMods.Paths.Extensions;

namespace NexusMods.Games.BethesdaGameStudios;

public class PluginAnalyzer : IFileAnalyzer
{
    public FileAnalyzerId Id { get; } = FileAnalyzerId.New("9c673a4f-064f-4b1e-83e3-4bf0454575cd", 1);

    public IEnumerable<FileType> FileTypes => new[] { FileType.TES4 };

#pragma warning disable CS1998
    public async IAsyncEnumerable<IFileAnalysisData> AnalyzeAsync(FileAnalyzerInfo info, [EnumeratorCancellation] CancellationToken ct = default)
#pragma warning restore CS1998
    {
        var overlay = SkyrimMod.CreateFromBinaryOverlay(info.Stream, SkyrimRelease.SkyrimSE, new ModKey("Dummy", ModType.Plugin));

        var masters = overlay.ModHeader.MasterReferences.Select(x => x.Master.FileName.ToString());
        yield return new PluginAnalysisData
        {
            IsLightMaster = overlay.ModHeader.Flags.HasFlag(SkyrimModHeader.HeaderFlag.LightMaster),
            Masters = masters.Select(m => m.ToRelativePath()).ToArray()
        };
    }
}
