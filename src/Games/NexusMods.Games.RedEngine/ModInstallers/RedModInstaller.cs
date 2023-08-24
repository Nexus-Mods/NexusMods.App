using System.Text.Json;
using System.Text.Json.Serialization;
using NexusMods.Common;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.ArchiveContents;
using NexusMods.DataModel.Extensions;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.ModInstallers;
using NexusMods.Games.RedEngine.FileAnalyzers;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;
using NexusMods.Paths.FileTree;

namespace NexusMods.Games.RedEngine.ModInstallers;

public class RedModInstaller : IModInstaller, IModInstallerEx
{
    private static readonly RelativePath InfoJson = "info.json".ToRelativePath();
    private static readonly RelativePath Mods = "mods".ToRelativePath();

    private static bool IsInfoJson(KeyValuePair<RelativePath, AnalyzedFile> file)
    {
        return file.Key.FileName == FileAnalyzers.InfoJson && file.Value.AnalysisData.OfType<RedModInfo>().Any();
    }

    public ValueTask<IEnumerable<ModInstallerResult>> GetModsAsync(
        GameInstallation gameInstallation,
        ModId baseModId,
        Hash srcArchiveHash,
        EntityDictionary<RelativePath, AnalyzedFile> archiveFiles,
        CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(GetMods(baseModId, srcArchiveHash, archiveFiles));
    }

    private IEnumerable<ModInstallerResult> GetMods(
        ModId baseModId,
        Hash srcArchiveHash,
        EntityDictionary<RelativePath, AnalyzedFile> archiveFiles)
    {
        var modFiles = archiveFiles
            .Where(IsInfoJson)
            .SelectMany(infoJson =>
            {
                var parent = infoJson.Key.Parent;
                var parentName = parent.FileName;

                return archiveFiles
                    .Where(kv => kv.Key.InFolder(parent))
                    .Select(kv =>
                    {
                        var (path, file) = kv;
                        return file.ToFromArchive(
                            new GamePath(GameFolderType.Game, Mods.Join(parentName).Join(path.RelativeTo(parent)))
                        );
                    });
            })
            .ToArray();

        if (!modFiles.Any())
            yield break;

        yield return new ModInstallerResult
        {
            Id = baseModId,
            Files = modFiles
        };
    }

    public async ValueTask<IEnumerable<ModInstallerResult>> GetModsAsyncEx(GameInstallation gameInstallation, ModId baseModId, Hash srcArchiveHash,
        FileTreeNode<RelativePath, ModSourceFileEntry> archiveFiles, CancellationToken cancellationToken = default)
    {
        var infos = archiveFiles.GetAllDescendentFiles()
            .Where(f => f.Path.FileName == InfoJson)
            .SelectAsync(async f => (File: f, InfoJson: await ReadInfoJson(f.Value)))
            .Where(node => node.InfoJson != null)

    }

    private async Task<InfoJson?> ReadInfoJson(ModSourceFileEntry entry)
    {
        await using var stream = await entry.Open();
        return await JsonSerializer.DeserializeAsync<InfoJson>(stream);
    }


}

internal class InfoJson
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}
