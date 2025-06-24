using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Library.Installers;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Games.RedEngine.Cyberpunk2077.Models;
using NexusMods.Hashing.xxHash3;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;
using NexusMods.Paths.Trees;
using NexusMods.Paths.Trees.Traits;
using NexusMods.Sdk.IO;
using NexusMods.Sdk.FileStore;

namespace NexusMods.Games.RedEngine.ModInstallers;

public class RedModInstaller : ALibraryArchiveInstaller
{
    public RedModInstaller(IServiceProvider serviceProvider) : base(serviceProvider, serviceProvider.GetRequiredService<ILogger<RedModInstaller>>())
    {
        _fileStore = serviceProvider.GetRequiredService<IFileStore>();
    }
    
    private static readonly RelativePath InfoJson = "info.json";
    private static readonly RelativePath Mods = "mods";
    private readonly IFileStore _fileStore;
    
    private async Task<RedModInfo?> ReadInfoJson(Hash hash, IStreamFactory? streamFactory = null)
    {
        try
        {
            await using var stream = await _fileStore.GetFileStream(hash);
            return await JsonSerializer.DeserializeAsync<RedModInfo>(stream);
        }
        catch (Exception ex)
        {
            // TODO: Remove this after we get rid of the old mod code
            if (streamFactory != null)
            {
                await using var streamDirect = await streamFactory.GetStreamAsync();
                return await JsonSerializer.DeserializeAsync<RedModInfo>(streamDirect);
            }
            Logger.LogError(ex, "Failed to read info.json for {Hash}", hash);
            return null;
        }
    }
    
    public override async ValueTask<InstallerResult> ExecuteAsync(
        LibraryArchive.ReadOnly libraryArchive,
        LoadoutItemGroup.New loadoutGroup,
        ITransaction tx,
        Loadout.ReadOnly loadout,
        CancellationToken cancellationToken)
    {
        var tree = libraryArchive.GetTree();
        var infosList = new List<(KeyedBox<RelativePath, LibraryArchiveTree>  File, RedModInfo InfoJson)>();
        
        foreach (var f in tree.GetFiles())
        {
            if (f.Key().FileName != InfoJson)
                continue;

            var infoJson = await ReadInfoJson(f.Item.LibraryFile.Value.Hash);
            if (infoJson != null)
                infosList.Add((f, infoJson));
        }
        
        if (infosList.Count == 0) return new NotSupported(Reason: $"Archive contains no valid redmod files named `{InfoJson}`");

        foreach (var (file, infoJson) in infosList.OrderBy(x => x.InfoJson.Name))
        {
            var modFolder = file.Parent();
            var parentName = modFolder!.Segment();
            
            var loadoutItem = new LoadoutItem.New(tx)
            {
                LoadoutId = loadout.Id,
                Name = infoJson.Name,
                ParentId = loadoutGroup.Id,
            };
            
            var groupItem = new LoadoutItemGroup.New(tx, loadoutItem.Id)
            {
                LoadoutItem = loadoutItem,
                IsGroup = true,
            };

 
            RedModInfoFile.New redModInfoFile = null!;
            
            foreach (var childNode in modFolder!.GetFiles().OrderBy(f => f.Item.Path))
            {
                var relativePath = childNode.Item.Path.RelativeTo(modFolder!.Item.Path);
                var joinedPath = Mods.Join(parentName).Join(relativePath);
                
                var newFile = childNode.ToLoadoutFile(loadout.Id, groupItem.Id, tx, new GamePath(LocationId.Game, joinedPath));

                if (file.Item.Path == childNode.Item.Path)
                {
                    redModInfoFile = new RedModInfoFile.New(tx, newFile.Id)
                    {
                        Name = infoJson.Name,
                        Version = infoJson.Version,
                        LoadoutFile = newFile,
                    };
                }
            }
            
            if (redModInfoFile == null)
                throw new InvalidOperationException("Failed to find the info.json file in the mod archive, this should never happen");
            
            var redModItem = new RedModLoadoutGroup.New(tx, loadoutItem.Id)
            {
                LoadoutItemGroup = groupItem,
                RedModInfoFileId = redModInfoFile.Id,
            };
        }

        return new Success();
    }
}

internal class RedModInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;
    
    [JsonPropertyName("description")]
    public string? Description { get; set; } = string.Empty;
}
