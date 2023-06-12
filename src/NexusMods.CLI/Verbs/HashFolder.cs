using NexusMods.CLI.DataOutputs;
using NexusMods.DataModel;
using NexusMods.Paths;

namespace NexusMods.CLI.Verbs;

// ReSharper disable once ClassNeverInstantiated.Global
/// <summary>
/// Hashes the contents of a directory, caching the results
/// </summary>
public class HashFolder : AVerb<AbsolutePath>
{
    private readonly FileHashCache _cache;
    private readonly IRenderer _renderer;

    /// <summary>
    /// DI constructor
    /// </summary>
    /// <param name="cache"></param>
    /// <param name="configurator"></param>
    public HashFolder(FileHashCache cache, Configurator configurator)
    {
        _cache = cache;
        _renderer = configurator.Renderer;
    }

    /// <inheritdoc />
    public static VerbDefinition Definition => new("hash-folder",
        "Hashes the contents of a directory, caching the results",
        new OptionDefinition[]
        {
            new OptionDefinition<AbsolutePath>( "f", "folder", "Folder to hash")
        });

    /// <inheritdoc />
    public async Task<int> Run(AbsolutePath folder, CancellationToken token)
    {
        var rows = new List<object[]>();
        await _renderer.WithProgress(token, async () =>
        {
            await foreach (var r in _cache.IndexFolderAsync(folder, token).WithCancellation(token))
                rows.Add(new object[] { r.Path.RelativeTo(folder), r.Hash, r.Size, r.LastModified });

            return rows;
        });

        var results = new Table(
            Columns: new[] { "Path", "Hash", "Size", "LastModified" },
            Rows: rows.OrderBy(r => (RelativePath)r.First())
        );

        await _renderer.Render(results);
        return 0;
    }
}
