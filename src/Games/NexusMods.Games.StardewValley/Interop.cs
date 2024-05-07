using System.Text;
using NexusMods.Abstractions.IO;
using NexusMods.Abstractions.Loadouts.Extensions;
using NexusMods.Abstractions.Loadouts.Files;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.Games.StardewValley.Models;
using StardewModdingAPI.Toolkit.Framework.ModData;
using StardewModdingAPI.Toolkit.Serialization;
using StardewModdingAPI.Toolkit.Serialization.Models;

namespace NexusMods.Games.StardewValley;

/// <summary>
/// Interop to the SMAPI libraries
/// </summary>
internal static class Interop
{
    internal static readonly JsonHelper SMAPIJsonHelper = new();

    private static async ValueTask<T?> Deserialize<T>(Stream stream) where T : notnull
    {
        using var sr = new StreamReader(stream, Encoding.UTF8);
        var json = await sr.ReadToEndAsync();

        var res = SMAPIJsonHelper.Deserialize<T>(json);
        return res;
    }

    private static async ValueTask<T?> DeserializeWithDefaults<T>(Stream stream) where T : notnull
    {
        using var sr = new StreamReader(stream, Encoding.UTF8);
        var json = await sr.ReadToEndAsync();

        var res = Newtonsoft.Json.JsonConvert.DeserializeObject<T>(json);
        return res;
    }

    public static ValueTask<Manifest?> DeserializeManifest(Stream stream) => Deserialize<Manifest>(stream);

    public static async ValueTask<Manifest?> GetManifest(IFileStore fileStore, Mod.Model mod, CancellationToken cancellationToken = default)
    {
        var manifestFile = mod.Files.FirstOrDefault(f => f.HasMetadata(SMAPIManifestMetadata.SMAPIManifest));
        if (manifestFile == null || !manifestFile.TryGetAsStoredFile(out var storedFile)) return null;

        await using var stream = await fileStore.GetFileStream(storedFile.Hash, cancellationToken);
        return await DeserializeManifest(stream);
    }

    public static async ValueTask<ModDatabase?> GetModDatabase(
        IFileStore fileStore,
        Mod.Model smapi,
        CancellationToken cancellationToken = default)
    {
        var manifestFile = smapi.Files.FirstOrDefault(f => f.HasMetadata(SMAPIModDatabaseMarker.SMAPIModDatabase));
        if (manifestFile == null || !manifestFile.TryGetAsStoredFile(out var storedFile)) return null;

        // https://github.com/Pathoschild/SMAPI/blob/e8a86a0b98061d322c2af89af845ed9f5fd15468/src/SMAPI.Toolkit/ModToolkit.cs#L66-L71
        await using var stream = await fileStore.GetFileStream(storedFile.Hash, cancellationToken);
        var metadata = await DeserializeWithDefaults<MetadataModel>(stream);
        if (metadata is null) return null;

        var records = metadata.ModData.Select(kv => new ModDataRecord(kv.Key, kv.Value)).ToArray();
        return new ModDatabase(records, static _ => null);
    }
}
