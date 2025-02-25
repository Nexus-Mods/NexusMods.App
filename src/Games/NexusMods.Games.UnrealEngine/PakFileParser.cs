using CUE4Parse.Encryption.Aes;
using CUE4Parse.FileProvider;
using CUE4Parse.FileProvider.Objects;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Versions;

namespace NexusMods.Games.UnrealEngine;

public struct PakMetaData
{
    public GameFile[] PakAssets;
}

public static class PakFileParser
{
    public static PakMetaData ParsePakMeta(string pakFileData, VersionContainer? versionContainer, FAesKey? key)
    {
        var provider = new StreamedFileProvider(pakFileData, false, versionContainer);
        provider.Initialize();
        if (key != null) provider.SubmitKey(new FGuid(), key);
        return new PakMetaData()
        {
            PakAssets = provider.Files.Values.ToArray(),
        };
    }
}
