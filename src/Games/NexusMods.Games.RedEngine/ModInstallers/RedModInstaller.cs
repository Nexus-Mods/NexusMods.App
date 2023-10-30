using System.Text.Json;
using System.Text.Json.Serialization;
using NexusMods.Common;
using NexusMods.DataModel.Extensions;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.ModInstallers;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;
using NexusMods.Paths.FileTree;

namespace NexusMods.Games.RedEngine.ModInstallers;

public class RedModInstaller : IModInstaller
{
    private static readonly RelativePath InfoJson = "info.json".ToRelativePath();
    private static readonly RelativePath Mods = "mods".ToRelativePath();

    public async ValueTask<IEnumerable<ModInstallerResult>> GetModsAsync(
        GameInstallation gameInstallation,
        LoadoutId loadoutId,
        ModId baseModId,
        FileTreeNode<RelativePath, ModSourceFileEntry> archiveFiles,
        CancellationToken cancellationToken = default)
    {
        var infos = (await archiveFiles.GetAllDescendentFiles()
                .Where(f => f.Path.FileName == InfoJson)
                .SelectAsync(async f => (File: f, InfoJson: await ReadInfoJson(f.Value!)))
                .ToArrayAsync())
            .Where(node => node.InfoJson != null)
            .ToArray();


        List<ModInstallerResult> results = new();

        var baseIdUsed = false;
        foreach (var node in infos)
        {
            var modFolder = node.File.Parent;
            var parentName = modFolder.Name;
            var files = new List<AModFile>();
            foreach (var childNode in modFolder.GetAllDescendentFiles())
            {
                var path = childNode.Path;
                var entry = childNode.Value;
                files.Add(entry!.ToStoredFile(new GamePath(LocationId.Game, Mods.Join(parentName).Join(path.RelativeTo(modFolder.Path)))));

            }

            results.Add(new ModInstallerResult
            {
                Id = baseIdUsed ? ModId.New() : baseModId,
                Files = files,
                Name = node.InfoJson?.Name ?? "<unknown>"
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
