using CUE4Parse.Encryption.Aes;
using CUE4Parse.FileProvider;
using CUE4Parse.FileProvider.Objects;
using CUE4Parse.UE4.Objects.Core.Misc;
using DynamicData;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.IO;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Games.UnrealEngine.Interfaces;
using NexusMods.Paths;

namespace NexusMods.Games.UnrealEngine;

public struct PakMetaData
{
    public LocationId MountPoint;
    public GameFile[] PakAssets;
}

public static class PakFileParser
{
    public static async Task<Dictionary<string, PakMetaData>> ExtractAndParse(
        IUnrealEngineGameAddon ueGameAddon,
        TemporaryFileManager temporaryFileManager,
        IFileStore fileStore,
        LibraryArchive.ReadOnly archive,
        CancellationToken cancellationToken)
    {
        var pakMetadata = new Dictionary<string, PakMetaData>();
        var filesByFileName = Utils.GroupFilesByFileName(archive);
        foreach (var (key, entries) in filesByFileName)
        {
            var tempFolder = await CreateTempFiles(fileStore, temporaryFileManager, entries);
            var metaData = await ParsePakMeta(ueGameAddon, tempFolder.Path.ToString());
            pakMetadata.Add(key, metaData);
            await tempFolder.DisposeAsync();   
        }
        return pakMetadata;
    }
    
    private static LocationId IdentifyMountPoint(GameFile[] files)
    {
        var ue4ssMountPoints = new RelativePath[] { "Mods", "LogicMods" };
        var mountPoint = files
            .Select(file => file.Path)
            .FirstOrDefault(filePath =>
                {
                    var segments = new RelativePath(filePath).Parts;
                    return ue4ssMountPoints.Any(segments.Contains);
                }
            );
        return mountPoint != null ? Constants.LogicModsLocationId : Constants.PakModsLocationId;
    }
    
    private static async Task<TemporaryPath> CreateTempFiles(
        IFileStore fileStore,
        TemporaryFileManager tfs,
        LibraryFile.ReadOnly[] fileEntries)
    {
        var folder = tfs.CreateFolder("TempUEFolder_");
        foreach (var fileEntry in fileEntries)
        {
            var path = folder.Path.Combine(fileEntry.FileName);
            var stream = await fileStore.GetFileStream(fileEntry.Hash, token: CancellationToken.None);
            await using var fs = path.Create();
            await stream.CopyToAsync(fs);
        }

        return folder;
    }
    
    private static async ValueTask<PakMetaData> ParsePakMeta(
        IUnrealEngineGameAddon ueAddon,
        string pakFilePath)
    {
        var provider = new DefaultFileProvider(
            pakFilePath!,
            SearchOption.TopDirectoryOnly,
            false,
            ueAddon.VersionContainer);
        
        provider.Initialize();
        var keys = ueAddon.AESKeys?.Select(key => new KeyValuePair<FGuid, FAesKey>(new FGuid(), key));
        if (keys != null) await provider.SubmitKeysAsync(keys);
        var gameFiles = provider.Files.Values.DistinctBy(x => x.Name).ToArray();
        return new PakMetaData()
        {
            MountPoint = IdentifyMountPoint(gameFiles),
            PakAssets = gameFiles.ToArray(),
        };
    }
}
