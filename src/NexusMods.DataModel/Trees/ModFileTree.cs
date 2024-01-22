using NexusMods.Common;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Abstractions.DTOs;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.ModFiles;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;
using NexusMods.Paths.Trees;
using NexusMods.Paths.Trees.Traits;

namespace NexusMods.DataModel.Trees;

/// <summary>
///     Represents a tree of files sourced from a downloaded mod.
/// </summary>
/// <remarks>
///     See this for reference https://github.com/Nexus-Mods/NexusMods.Paths/blob/main/docs/Trees/Introduction.md
/// </remarks>
public struct ModFileTree :
    IHaveBoxedChildrenWithKey<RelativePath, ModFileTree>, // basic functionality
    IHaveAFileOrDirectory, // for uses which want to distinguish file/directory
    IHaveParent<ModFileTree>, // optimized FindSubPathsByKeyUpward
    IHaveDepthInformation, // Depth info is used by some installers, and it's zero-cost to include, thanks to leftover padding space
    IHavePathSegment, // optimized path segment based operations
    IHaveKey<RelativePath>,
    IHaveValue<ModFileTree>
{
    // +16 bytes due to Boxing/Class overhead. object overhead

    /// <inheritdoc />
    public Box<ModFileTree>? Parent { get; private set; } // 0

    /// <inheritdoc />
    public Dictionary<RelativePath, KeyedBox<RelativePath, ModFileTree>> Children { get; private set; } // 8

    /// <inheritdoc />
    public bool IsFile { get; private set; } // 16

    /// <inheritdoc />
    public ushort Depth { get; private set;  } // 17

    // Padding Available: 19-23 (inclusive)

    // Key

    /// <inheritdoc />
    public RelativePath Segment { get; init; } // 24

    // Below fields are non-interface fields. They are copied here directly for performance reasons.

    /// <summary>
    ///     Hash of the file.
    /// </summary>
    public Hash Hash => Hash.From(_hash); // 32
    private ulong _hash; // Vogen has overhead on object size , so we store raw.

    /// <summary>
    ///     Size of the file.
    /// </summary>
    public Size Size => Size.From(_size); // 40
    private ulong _size; // Vogen has overhead on object size, so we store raw.

    /// <summary>
    ///     A factory that can be used to open the file and read its contents
    /// </summary>
    public required IStreamFactory? StreamFactory { get; init; } // 48

    // Struct end at 56 bytes.

    /// <summary>
    ///     Open the file as a readonly seekable stream
    /// </summary>
    public ValueTask<Stream> OpenAsync()
    {
        return StreamFactory!.GetStreamAsync();
    }

    /// <summary>
    ///     Maps the current node to a <see cref="StoredFile" /> mod file
    /// </summary>
    /// <param name="to">Destination path of the mod file.</param>
    public StoredFile ToStoredFile(GamePath to)
    {
        return new StoredFile
        {
            Id = ModFileId.NewId(),
            To = to,
            Hash = Hash,
            Size = Size
        };
    }

    // Utility Methods/Properties

    /// <summary>
    ///     The complete path of the file or directory.
    /// </summary>
    public RelativePath Path => this.ReconstructPath();

    /// <summary>
    ///     The name file or directory in this node.
    /// </summary>
    public RelativePath FileName => Segment;

    /// <summary>
    ///     The extension of this node.
    /// </summary>
    public Extension Extension => Segment.Extension;

    // Interface Redirects
    public RelativePath Key => Segment;
    public ModFileTree Value => this;

    /// <summary>
    ///     Creates the tree! From the download content entries.
    /// </summary>
    /// <param name="downloads">Downloads from the download registry.</param>
    /// <param name="fs">FileStore to read the files from.</param>
    public static KeyedBox<RelativePath, ModFileTree> Create(IReadOnlyCollection<DownloadContentEntry> downloads, IFileStore? fs = null)
    {
        var entries = GC.AllocateUninitializedArray<ModFileTreeSource>(downloads.Count);
        var entryIndex = 0;
        foreach (var dl in downloads)
            entries[entryIndex++] = new ModFileTreeSource(dl, fs != null ? new ArchiveManagerStreamFactory(fs, dl.Hash) { Name = dl.Path, Size = dl.Size } : null);

        return Create(entries);
    }

    /// <summary>
    ///     Creates the tree! From the source entries.
    /// </summary>
    public static KeyedBox<RelativePath, ModFileTree> Create(ModFileTreeSource[] entries)
    {
        // Unboxed root node.
        var root = CreateDirectoryNode(RelativePath.Empty, 0);

        // Add each entry to the tree.
        foreach (var entry in entries)
        {
            var path = entry.Path;
            var current = root;
            var parts = path.GetParts();

            for (var x = 0; x < parts.Length; x++)
            {
                var segment = parts[x];
                var isFile = x == parts.Length - 1;

                // Try get child for this segment.
                if (!current.Item.Children.TryGetValue(segment, out var child))
                {
                    var depth = (ushort)(x + 1);
                    child = isFile ? CreateFileNode(segment, entry.Hash, entry.Size, true, depth, entry.Factory, current) : CreateDirectoryNode(segment, depth, current);
                    current.Item.Children.Add(segment, child);
                }

                current = child;
            }
        }

        return root;
    }

    private static KeyedBox<RelativePath, ModFileTree> CreateDirectoryNode(RelativePath segmentName, ushort depth, Box<ModFileTree>? parent = null)
        => CreateFileNode(segmentName, 0, 0, false, depth, null, parent);

    private static KeyedBox<RelativePath, ModFileTree> CreateFileNode(RelativePath segmentName, ulong hash, ulong size, bool isFile, ushort depth, IStreamFactory? factory,
        Box<ModFileTree>? parent)
    {
        return new KeyedBox<RelativePath, ModFileTree>
        {
            Item = new ModFileTree
            {
                Segment = segmentName,
                Children = new Dictionary<RelativePath, KeyedBox<RelativePath, ModFileTree>>(),
                IsFile = isFile,
                Parent = parent,
                _hash = hash,
                _size = size,
                StreamFactory = factory,
                Depth = depth,
            }
        };
    }
}

/// <summary>
///     Extension methods for <see cref="ModFileTree" />.
/// </summary>
public static class ModFileTreeExtensions
{
    /// <summary>
    ///     Maps the ModFileTree item within the KeyedBox to a <see cref="StoredFile"/>.
    /// </summary>
    /// <param name="keyedBox">The KeyedBox containing a ModFileTree instance.</param>
    /// <param name="to">Destination path of the mod file.</param>
    /// <returns>A new StoredFile instance.</returns>
    public static StoredFile ToStoredFile(this KeyedBox<RelativePath, ModFileTree> keyedBox, GamePath to) => keyedBox.Item.ToStoredFile(to);

    /// <summary>
    ///     Gets the file extension of the ModFileTree item within the KeyedBox.
    /// </summary>
    /// <param name="keyedBox">The KeyedBox containing a ModFileTree instance.</param>
    /// <returns>The file extension.</returns>
    public static Extension Extension(this KeyedBox<RelativePath, ModFileTree> keyedBox) => keyedBox.Item.Extension;

    /// <summary>
    ///     Gets the file name of the ModFileTree item within the KeyedBox.
    /// </summary>
    /// <param name="keyedBox">The KeyedBox containing a ModFileTree instance.</param>
    /// <returns>The file name.</returns>
    public static RelativePath FileName(this KeyedBox<RelativePath, ModFileTree> keyedBox) => keyedBox.Item.FileName;

    /// <summary>
    ///     Gets the complete path of the ModFileTree item within the KeyedBox.
    /// </summary>
    /// <param name="keyedBox">The KeyedBox containing a ModFileTree instance.</param>
    /// <returns>The complete path.</returns>
    public static RelativePath Path(this KeyedBox<RelativePath, ModFileTree> keyedBox) => keyedBox.Item.Path;
}
