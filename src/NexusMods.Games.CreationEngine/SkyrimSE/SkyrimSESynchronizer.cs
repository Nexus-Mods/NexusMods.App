using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Binary.Parameters;
using Mutagen.Bethesda.Plugins.Binary.Streams;
using NexusMods.Hashing.xxHash3;
using NexusMods.Paths;

namespace NexusMods.Games.CreationEngine.SkyrimSE;

public class SkyrimSESynchronizer : ACreationEngineSynchronizer
{
    private readonly GroupMask _headerGroupMask;

    public SkyrimSESynchronizer(IServiceProvider provider) : base(provider)
    {
        _headerGroupMask = new GroupMask(false);
    }

    /// <summary>
    /// Extremely efficient way to get the header for a plugin. The first invocation of this method will be slow
    /// (thanks to Mutagen's internal startup routines), but subsequent invocations will be fast. The routine
    /// only loads header of the file into memory.
    /// </summary>
    public async ValueTask<ISkyrimModHeaderGetter> HeaderForPlugin(Hash hash, RelativePath? path = null)
    {
        var fileName = path?.FileName.ToString() ?? "unknown.esm";
        var key = ModKey.FromFileName(fileName);
        await using var stream = await FileStore.GetFileStream(hash);
        var meta = ParsingMeta.Factory(BinaryReadParameters.Default, GameRelease.SkyrimSE, key, stream);
        await using var mutagenStream = new MutagenBinaryReadStream(stream, meta);
        using var frame = new MutagenFrame(mutagenStream);
        var mod = SkyrimMod.CreateFromBinary(frame, SkyrimRelease.SkyrimSE, _headerGroupMask);
        return mod.ModHeader;
    }
}
