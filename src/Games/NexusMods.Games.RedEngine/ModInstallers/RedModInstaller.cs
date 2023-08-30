using System.Text.Json;
using System.Text.Json.Serialization;
using NexusMods.Common;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.ArchiveContents;
using NexusMods.DataModel.Extensions;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.ModInstallers;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;
using NexusMods.Paths.FileTree;

namespace NexusMods.Games.RedEngine.ModInstallers;

public class RedModInstaller : IModInstaller
{
    private static readonly RelativePath InfoJson = "info.json".ToRelativePath();
    private static readonly RelativePath Mods = "mods".ToRelativePath();

    public async ValueTask<IEnumerable<ModInstallerResult>> GetModsAsync(GameInstallation gameInstallation, ModId baseModId,
        FileTreeNode<RelativePath, ModSourceFileEntry> archiveFiles, CancellationToken cancellationToken = default)
    {
        var infos = archiveFiles.GetAllDescendentFiles()
            .Where(f => f.Path.FileName == InfoJson)
            .SelectAsync(async f => (File: f, InfoJson: await ReadInfoJson(f.Value!)))
            .Where(node => node.InfoJson != null);


        List<ModInstallerResult> results = new();

        var baseIdUsed = false;
        await foreach (var node in infos)
        {
            var modFolder = node.File.Parent;
            var parentName = modFolder.Name;
            var files = new List<AModFile>();
            foreach (var childNode in modFolder.GetAllDescendentFiles())
            {
                var path = childNode.Path;
                var entry = childNode.Value;
                files.Add(entry!.ToFromArchive(new GamePath(GameFolderType.Game, Mods.Join(parentName).Join(path.RelativeTo(modFolder.Path)))));

            }

            results.Add(new ModInstallerResult
            {
                Id = baseIdUsed ? ModId.New() : baseModId,
                Files = files
            });
            baseIdUsed = true;
        }

        return results;
    }

    private static async Task<RedModInfo?> ReadInfoJson(ModSourceFileEntry entry)
    {
        await using var stream = await entry.Open();
        return await JsonSerializer.DeserializeAsync<RedModInfo>(stream);
    }

}

internal class RedModInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}
