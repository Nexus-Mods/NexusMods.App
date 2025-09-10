using Microsoft.Extensions.DependencyInjection;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Fallout4;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Binary.Parameters;
using Mutagen.Bethesda.Plugins.Binary.Streams;
using Mutagen.Bethesda.Plugins.Records;
using Mutagen.Bethesda.Skyrim;
using NexusMods.Games.CreationEngine.Abstractions;
using NexusMods.Hashing.xxHash3;
using NexusMods.Paths;
using NexusMods.Sdk.FileStore;
using Noggog;

namespace NexusMods.Games.CreationEngine;

public class PluginUtilities<TGame> : IPluginUtilities 
{
    private readonly IFileStore _fileStore;
    private readonly Mutagen.Bethesda.Skyrim.GroupMask _skyrimGroupMask;
    private readonly Mutagen.Bethesda.Fallout4.GroupMask _fallout4GroupMask;
    private readonly GameRelease _game;

    public PluginUtilities(IServiceProvider provider)
    {
        _fileStore = provider.GetRequiredService<IFileStore>();
        _skyrimGroupMask = new Mutagen.Bethesda.Skyrim.GroupMask(false);
        _fallout4GroupMask = new Mutagen.Bethesda.Fallout4.GroupMask(false);
        if (typeof(TGame) == typeof(SkyrimSE.SkyrimSE))
        {
            _game = GameRelease.SkyrimSE;
        }
        else if (typeof(TGame) == typeof(Fallout4.Fallout4))
        {
            _game = GameRelease.Fallout4;
        }
        else
        {
            throw new NotImplementedException($"Game {typeof(TGame).Name} not supported");
        }
    }
        
    
    /// <summary>
    /// Extremely efficient way to get the header for a plugin. The first invocation of this method will be slow
    /// (thanks to Mutagen's internal startup routines), but subsequent invocations will be fast. The routine
    /// only loads header of the file into memory.
    /// </summary>
    public async ValueTask<PluginHeader?> ParsePluginHeader(Hash hash, RelativePath? name = null)
    {
        var fileName = name?.FileName.ToString() ?? "unknown.esm";
        var key = ModKey.FromFileName(fileName);
        await using var stream = await _fileStore.GetFileStream(hash);
        var meta = ParsingMeta.Factory(BinaryReadParameters.Default, GameRelease.SkyrimSE, key, stream);
        await using var mutagenStream = new MutagenBinaryReadStream(stream, meta);
        using var frame = new MutagenFrame(mutagenStream);

        if (_game == GameRelease.SkyrimSE)
        {
            var mod = SkyrimMod.CreateFromBinary(frame, SkyrimRelease.SkyrimSE, _skyrimGroupMask);
            return new PluginHeader()
            {
                Key = mod.ModKey,
                Masters = ToMasterArray(mod.ModHeader.MasterReferences),
            };
        }
        else if (_game == GameRelease.Fallout4)
        {
            var mod = Fallout4Mod.CreateFromBinary(frame, Fallout4Release.Fallout4, _fallout4GroupMask);
            return new PluginHeader()
            {
                Key = mod.ModKey,
                Masters = ToMasterArray(mod.ModHeader.MasterReferences),
            };
        }
        throw new NotImplementedException("Game not supported");
    }

    private ModKey[] ToMasterArray(ExtendedList<MasterReference> modHeaderMasterReferences)
    {
        var result = GC.AllocateUninitializedArray<ModKey>(modHeaderMasterReferences.Count);
        for (var i = 0; i < modHeaderMasterReferences.Count; i++)
        {
            result[i] = modHeaderMasterReferences[i].Master;
        }
        return result;
    }
}
