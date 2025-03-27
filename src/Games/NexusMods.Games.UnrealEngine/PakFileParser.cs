using CUE4Parse.Encryption.Aes;
using CUE4Parse.FileProvider;
using CUE4Parse.FileProvider.Objects;
using CUE4Parse.UE4.Objects.Core.Misc;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.IO;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Games.UnrealEngine.Interfaces;
using NexusMods.MnemonicDB.Abstractions.Models;
using NexusMods.Paths;

namespace NexusMods.Games.UnrealEngine;

public struct PakMetaData
{
    public LocationId MountPoint;
    public GameFile[] PakAssets;
}

public static class PakFileParser
{
    public static async Task<PakMetaData> Deserialize(
        IUnrealEngineGameAddon ueGameAddon,
        TemporaryFileManager temporaryFileManager,
        IFileStore fileStore,
        LibraryFile.ReadOnly fileEntry,
        CancellationToken cancellationToken)
    {
        var tempFile = temporaryFileManager.CreateFile(Constants.PakExt);
        await using (var stream = await fileStore.GetFileStream(fileEntry.Hash, token: cancellationToken))
        {
            await using var fs = tempFile.Path.Open(FileMode.Create, FileAccess.ReadWrite, FileShare.None);
            await stream.CopyToAsync(fs, cancellationToken);
        }
        var metaData = await ParsePakMeta(ueGameAddon, tempFile.Path.ToString());
        await tempFile.DisposeAsync();
        return metaData;
    }

    public static async ValueTask<PakMetaData> ParsePakMeta(
        IUnrealEngineGameAddon ueAddon,
        string pakFilePath)
    {
        var provider = new DefaultFileProvider(
            Path.GetDirectoryName(pakFilePath)!,
            SearchOption.TopDirectoryOnly,
            false,
            ueAddon.VersionContainer);

        provider.Initialize();
        var keys = ueAddon.AESKeys?.Select(key => new KeyValuePair<FGuid, FAesKey>(new FGuid(), key));
        if (keys != null) await provider.SubmitKeysAsync(keys);
        var gameFiles = provider.Files.Values.ToArray();
        return new PakMetaData()
        {
            MountPoint = IdentifyMountPoint(gameFiles),
            PakAssets = gameFiles.ToArray(),
        };
    }
    
    private static LocationId IdentifyMountPoint(GameFile[] files)
    {
        var mountPoint = files
            .Select(file => file.Path)
            .Where(filePath =>
                {
                    var segments = filePath.Split(Path.DirectorySeparatorChar);
                    return new[] { "Mods", "LogicMods" }.Any(segments.Contains);
                }
            )
            .FirstOrDefault();
        return mountPoint != null ? Constants.LogicModsLocationId : Constants.PakModsLocationId;
    }
}
