using NexusMods.Archives.Nx.FileProviders;
using NexusMods.Archives.Nx.Packing.Unpack;
using NexusMods.Paths;
using NexusMods.Paths.FileProviders;

namespace NexusMods.Games.IntegrationTestFramework;

public class NxReadOnlyFilesystem : IReadOnlyFileSource
{
    private readonly AbsolutePath _mountPath;
    private readonly AbsolutePath _source;
    private readonly FromFilePathProvider _provider;
    private readonly PathedFileEntry[] _entries;
    private readonly Dictionary<string, PathedFileEntry> _lookup;

    public NxReadOnlyFilesystem(AbsolutePath source, AbsolutePath mountPoint)
    {
        _mountPath = mountPoint;
        _source = source;
        try
        {
            _provider = new FromFilePathProvider { FilePath = source.ToString() };
            _entries = new NxUnpacker(_provider).GetPathedFileEntries();
            _lookup = _entries.ToDictionary(x => x.FilePath, x => x);
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to open Nx archive at {source}", ex);
        }

    }
    
    public IEnumerable<RelativePath> EnumerateFiles()
    {
        throw new NotImplementedException();
    }

    public Stream OpenRead(RelativePath relativePath)
    {
        throw new NotImplementedException();
    }

    public AbsolutePath MountPoint => _mountPath;
}
