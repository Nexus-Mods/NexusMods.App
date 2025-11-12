using DynamicData.Kernel;
using NexusMods.Paths;
using NexusMods.Paths.Trees;
using NexusMods.Paths.Trees.Traits;

namespace NexusMods.Sdk.Library;
using LibraryArchiveTreeNode = Paths.Trees.KeyedBox<Paths.RelativePath, LibraryArchiveTree>;

/// <summary>
///     Represents a tree of files sourced from a downloaded mod.
/// </summary>
/// <remarks>
///     See this for reference https://github.com/Nexus-Mods/NexusMods.Paths/blob/main/docs/Trees/Introduction.md
/// </remarks>
public struct LibraryArchiveTree :
    IHaveBoxedChildrenWithKey<RelativePath, LibraryArchiveTree>, // basic functionality
    IHaveAFileOrDirectory, // for uses which want to distinguish file/directory
    IHaveParent<LibraryArchiveTree>, // optimized FindSubPathsByKeyUpward
    IHaveDepthInformation, // Depth info is used by some installers, and it's zero-cost to include, thanks to leftover padding space
    IHavePathSegment, // optimized path segment based operations
    IHaveKey<RelativePath>,
    IHaveValue<LibraryArchiveTree>
{
    /// <inheritdoc />
    public Box<LibraryArchiveTree>? Parent { get; private set; } // 0

    /// <inheritdoc />
    public Dictionary<RelativePath, LibraryArchiveTreeNode> Children { get; private set; } // 8
    
    /// <inheritdoc />
    public ushort Depth { get; private set;  } // 17
    
    /// <summary>
    /// True if this node represents a file.
    /// </summary>
    public bool IsFile => LibraryFile.HasValue;

    /// <inheritdoc />
    public RelativePath Segment { get; init; } // 24

    /// <summary>
    /// The library file, this node represents.
    /// </summary>
    public Optional<LibraryFile.ReadOnly> LibraryFile { get; init; }
    
    /// <summary>
    ///     The complete path of the file or directory.
    /// </summary>
    public RelativePath Path => this.ReconstructPath();

    /// <summary>
    ///     The name file or directory in this node.
    /// </summary>
    public RelativePath FileName => Segment;

    // Interface Redirects
    /// <inheritdoc />
    public RelativePath Key => Segment;

    /// <inheritdoc />
    public LibraryArchiveTree Value => this;

    /// <summary>
    ///     Creates the tree! From the source entries.
    /// </summary>
    public static LibraryArchiveTreeNode Create(LibraryArchive.ReadOnly archive)
    {
        // Unboxed root node.
        var root = CreateDirectoryNode(RelativePath.Empty, 0);

        // Add each entry to the tree.
        foreach (var entry in archive.Children)
        {
            var libraryFile = entry.AsLibraryFile();
            var current = root;
            var parts = entry.Path.GetParts();

            for (var x = 0; x < parts.Length; x++)
            {
                var segment = parts[x];
                var isFile = x == parts.Length - 1;

                // Try get child for this segment.
                if (!current.Item.Children.TryGetValue(segment, out var child))
                {
                    var depth = (ushort)(x + 1);
                    child = isFile ? CreateFileNode(segment, libraryFile, depth, current) : CreateDirectoryNode(segment, depth, current);
                    current.Item.Children.Add(segment, child);
                }

                current = child;
            }
        }

        return root;
    }

    private static LibraryArchiveTreeNode CreateDirectoryNode(RelativePath segmentName, ushort depth, Box<LibraryArchiveTree>? parent = null)
        => CreateFileNode(segmentName, Optional<LibraryFile.ReadOnly>.None, depth, parent);

    private static LibraryArchiveTreeNode CreateFileNode(RelativePath segmentName, Optional<LibraryFile.ReadOnly> libraryFile, ushort depth, Box<LibraryArchiveTree>? parent)
    {
        return new LibraryArchiveTreeNode
        {
            Item = new LibraryArchiveTree
            {
                Segment = segmentName,
                Children = new Dictionary<RelativePath, LibraryArchiveTreeNode>(),
                Parent = parent,
                LibraryFile = libraryFile,
                Depth = depth,
            },
        };
    }

}

/// <summary>
/// Converters and extensions for <see cref="LibraryArchiveTree"/>.
/// </summary>
public static class LibraryArchiveTreeExtensions
{
    /// <summary>
    /// Organize the children of this node into a tree based on their paths.
    /// </summary>
    public static LibraryArchiveTreeNode GetTree(this LibraryArchive.ReadOnly archive)
        => LibraryArchiveTree.Create(archive);
}
