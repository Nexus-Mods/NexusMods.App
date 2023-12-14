using System.Text.Json;
using System.Text.Json.Serialization;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.ModInstallers;
using NexusMods.DataModel.Trees;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;
using NexusMods.Paths.Trees;
using NexusMods.Paths.Trees.Traits;

namespace NexusMods.Games.RedEngine.ModInstallers;

public class RedModInstaller : IModInstaller
{
    private static readonly RelativePath InfoJson = "info.json".ToRelativePath();
    private static readonly RelativePath Mods = "mods".ToRelativePath();

    public async ValueTask<IEnumerable<ModInstallerResult>> GetModsAsync(
        GameInstallation gameInstallation,
        LoadoutId loadoutId,
        ModId baseModId,
        KeyedBox<RelativePath, ModFileTree> archiveFiles,
        CancellationToken cancellationToken = default)
    {
        var infosList = new List<(KeyedBox<RelativePath, ModFileTree>  File, RedModInfo? InfoJson)>();
        foreach (var f in archiveFiles.GetFiles())
        {
            if (f.FileName() != InfoJson)
                continue;

            var infoJson = await ReadInfoJson(f);
            if (infoJson != null)
                infosList.Add((f, infoJson));
        }

        List<ModInstallerResult> results = new();
        var baseIdUsed = false;
        foreach (var node in infosList)
        {
            var modFolder = node.File.Parent();
            var parentName = modFolder!.Segment();
            var files = new List<AModFile>();
            foreach (var childNode in modFolder!.GetFiles())
                files.Add(childNode.ToStoredFile(new GamePath(LocationId.Game, Mods.Join(parentName).Join(childNode.Path().RelativeTo(modFolder!.Item.Path)))));

            results.Add(new ModInstallerResult
            {
                Id = baseIdUsed ? ModId.NewId() : baseModId,
                Files = files,
                Name = node.InfoJson?.Name ?? "<unknown>"
            });
            baseIdUsed = true;
        }

        return results;
    }

    private static async Task<RedModInfo?> ReadInfoJson(KeyedBox<RelativePath, ModFileTree> entry)
    {
        await using var stream = await entry.Item.OpenAsync();
        return await JsonSerializer.DeserializeAsync<RedModInfo>(stream);
    }

}

internal class RedModInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}
