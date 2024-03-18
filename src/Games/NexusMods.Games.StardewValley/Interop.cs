using System.Text;
using NexusMods.Abstractions.IO;
using NexusMods.Abstractions.Loadouts.Extensions;
using NexusMods.Abstractions.Loadouts.Files;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.Games.StardewValley.Models;
using StardewModdingAPI.Toolkit.Serialization;
using StardewModdingAPI.Toolkit.Serialization.Models;

namespace NexusMods.Games.StardewValley;

/// <summary>
/// Interop to the SMAPI libraries
/// </summary>
internal static class Interop
{
    internal static readonly JsonHelper SMAPIJsonHelper = new();

    public static async ValueTask<Manifest?> DeserializeManifest(Stream stream)
    {
        using var sr = new StreamReader(stream, Encoding.UTF8);
        var json = await sr.ReadToEndAsync();

        var manifest = SMAPIJsonHelper.Deserialize<Manifest>(json);
        return manifest;
    }

    public static async ValueTask<Manifest?> GetManifest(IFileStore fileStore, Mod mod, CancellationToken cancellationToken = default)
    {
        var manifestFile = mod.Files.Values.FirstOrDefault(f => f.HasMetadata<SMAPIManifestMetadata>());
        if (manifestFile is not StoredFile storedFile) return null;

        await using var stream = await fileStore.GetFileStream(storedFile.Hash, cancellationToken);
        return await DeserializeManifest(stream);
    }
}
