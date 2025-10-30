using System.Collections.Frozen;
using NexusMods.Archives.Nx.FileProviders;
using NexusMods.Archives.Nx.Headers;
using NexusMods.Archives.Nx.Packing.Unpack;
using NexusMods.DataModel;
using NexusMods.Paths;
using NexusMods.Paths.FileProviders;

namespace NexusMods.Games.IntegrationTestFramework;

public class NxReadOnlyFilesystem : IReadOnlyFileSource
{
    private readonly AbsolutePath _mountPath;
    private readonly AbsolutePath _source;
    private readonly FromFilePathProvider _provider;
    private readonly PathedFileEntry[] _entries;
    private readonly FrozenDictionary<RelativePath, PathedFileEntry> _lookup;

    public NxReadOnlyFilesystem(AbsolutePath source, AbsolutePath mountPoint)
    {
        _mountPath = mountPoint;
        _source = source;
        try
        {
            using var stream = File.OpenRead(source.ToString());
            //_provider = new FromFilePathProvider { FilePath = source.ToString() };
            _entries = new NxUnpacker(new FromStreamProvider(stream)).GetPathedFileEntries();
            _lookup = _entries.ToFrozenDictionary(x => RelativePath.FromUnsanitizedInput(x.FilePath), x => x);
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to open Nx archive at {source}", ex);
        }

    }
    
    public IEnumerable<RelativePath> EnumerateFiles()
    {
        return _lookup.Keys;
    }

    public Stream OpenRead(RelativePath relativePath)
    {
        if (!_lookup.TryGetValue(relativePath, out var entry))
            throw new FileNotFoundException();
        
        var file = _source.Read();

        var provider = new FromStreamProvider(file);
        var header = HeaderParser.ParseHeader(provider);

        return new ChunkedStream<ChunkedArchiveStream>(new ChunkedArchiveStream(entry.Entry, header, file));
    }

    public AbsolutePath MountPoint => _mountPath;
}
