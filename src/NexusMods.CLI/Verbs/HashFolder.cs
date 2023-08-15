using NexusMods.Abstractions.CLI;
using NexusMods.Abstractions.CLI.DataOutputs;
using NexusMods.DataModel;
using NexusMods.Paths;

namespace NexusMods.CLI.Verbs;

// ReSharper disable once ClassNeverInstantiated.Global
/// <summary>
/// Hashes the contents of a directory, caching the results
/// </summary>
public class HashFolder : AVerb<AbsolutePath>, IRenderingVerb
{
    private readonly FileHashCache _cache;

    /// <inheritdoc />
    public IRenderer Renderer { get; set; } = null!;

    /// <summary>
    /// DI constructor
    /// </summary>
    /// <param name="cache"></param>
    public HashFolder(FileHashCache cache)
    {
        _cache = cache;
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
        await Renderer.WithProgress(token, async () =>
        {
            await foreach (var r in _cache.IndexFolderAsync(folder, token).WithCancellation(token))
                rows.Add(new object[] { r.Path.RelativeTo(folder), r.Hash, r.Size, r.LastModified });

            return rows;
        });

        var results = new Table(
            Columns: new[] { "Path", "Hash", "Size", "LastModified" },
            Rows: rows.OrderBy(r => (RelativePath)r.First())
        );

        await Renderer.Render(results);
        return 0;
    }
}
