using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Library.Installers;
using NexusMods.Abstractions.Loadouts;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;
using NexusMods.Paths.Trees;
using NexusMods.Paths.Trees.Traits;
using NexusMods.Sdk;
using NexusMods.Sdk.Library;

namespace NexusMods.Games.Generic.Installers;

public class PredicateBasedInstaller : ALibraryArchiveInstaller
{
    public PredicateBasedInstaller(IServiceProvider serviceProvider) : base(serviceProvider, serviceProvider.GetRequiredService<ILogger<PredicateBasedInstaller>>())
    {
        
    }
    public required Func<Node, bool> Root { get; init; }
    public required GamePath Destination { get; init; }
    
    /// <summary>
    /// Files in this array will not be installed, and will not cause the installer to fail if they exist.
    /// </summary>
    public object[] IgnoreFiles { get; init; } = [];

    public ref struct Node
    {
        private readonly KeyValuePair<RelativePath, KeyedBox<RelativePath, LibraryArchiveTree>> _node;

        public Node(KeyValuePair<RelativePath, KeyedBox<RelativePath, LibraryArchiveTree>> node)
        {
            _node = node; 
        }

        /// <summary>
        /// True if this node is the root (no parent folder)
        /// </summary>
        public bool IsRoot => _node.Key == RelativePath.Empty;

        /// <summary>
        /// Returns true if this node has a direct child folder with the given path.
        /// </summary>
        public bool HasDirectChildFolder(RelativePath path) => _node.Value.Item.Children.TryGetValue(path, out var child) && !child.Item.IsFile;
        
        /// <summary>
        /// Returns true if this node has a direct child folder with any of the given paths.
        /// </summary>
        public bool HasAnyDirectChildFolder(params ReadOnlySpan<string> path)
        {
            foreach (var child in _node.Children())
            {
                if (child.IsFile())
                    continue;
                for (var i = 0; i < path.Length; i++)
                {
                    if (child.Key.ToString().EndsWith(path[i], StringComparison.InvariantCultureIgnoreCase))
                        return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Returns true if this node's name matches the given glob.
        /// </summary>
        public bool ThisNameLike(Regex regex) => regex.IsMatch(_node.Key.ToString());
        
        /// <summary>
        /// Returns true if this node's name matches the given name.'
        /// </summary>
        public bool ThisNameIs(string name) => !_node.Value.Item.IsFile && _node.Key == RelativePath.FromUnsanitizedInput(name);

        /// <summary>
        /// Returns true if this node has a direct child that ends with the given postfix.
        /// </summary>
        public bool HasDirectChildEndingIn(string postfix) => _node.Value.Item.Children.Any(c => c.Key.ToString().EndsWith(postfix, StringComparison.InvariantCultureIgnoreCase));
        
        /// <summary>
        /// Returns true if this node has a direct child with the given name.
        /// </summary>
        public bool HasDirectChild(string name) => _node.Value.Item.Children.Any(c => c.Key.ToString().Equals(name, StringComparison.InvariantCultureIgnoreCase));
        
        /// <summary>
        /// Returns true if any of the direct children of this node are files with the given extensions.
        /// </summary>
        public bool IsRootWith(params ReadOnlySpan<Extension> extensions)
        {
            if (_node.Key != RelativePath.Empty)
                return false;
            foreach (var child in _node.Value.Item.Children)
            {
                for (int extIdx = 0; extIdx < extensions.Length; extIdx++)
                {
                    if (!child.Value.Item.IsFile)
                        continue;
                    if (child.Key.Extension == extensions[extIdx])
                        return true;
                }
            }

            return false;
        }
        
        /// <summary>
        /// Returns true if any of the direct children of this node are files with the given extensions.
        /// </summary>
        public bool HasDirectChildrenWith(params ReadOnlySpan<Extension> extensions)
        {
            foreach (var child in _node.Value.Item.Children)
            {
                for (var extIdx = 0; extIdx < extensions.Length; extIdx++)
                {
                    if (!child.Value.Item.IsFile)
                        continue;
                    if (child.Key.Extension == extensions[extIdx])
                        return true;
                }
            }

            return false;
        }
    }

    public override ValueTask<InstallerResult> ExecuteAsync(LibraryArchive.ReadOnly libraryArchive, LoadoutItemGroup.New loadoutGroup, ITransaction transaction, Loadout.ReadOnly loadout, CancellationToken cancellationToken)
    {
        var tree = LibraryArchiveTreeExtensions.GetTree(libraryArchive);

        bool isFound = false;
        LibraryArchiveTree found = default!;
        if (Root(new Node(new KeyValuePair<RelativePath, KeyedBox<RelativePath, LibraryArchiveTree>>("", tree))))
        {
            isFound = true;
            found = tree;
        }
        else
        {
            isFound = tree
                .EnumerateChildrenBfs()
                .Where(x => Root(new Node(x)))
                .Select(x => x.Value.Item)
                .TryGetFirst(out found);
        }

        if (!isFound)
            return ValueTask.FromResult<InstallerResult>(new NotSupported(Reason: "Found no matching root"));

        var rootDestination = Destination;
        int handled = 0;
        int total = 0;
        foreach (var (_, libraryFile) in tree.EnumerateChildrenBfs())
        {
            if (!libraryFile.Item.IsFile) 
                continue;
            
            total++;
            if (!libraryFile.Item.Value.Path.InFolder(found.Path))
            {
                if (IgnoreFiles.Any(i => IsIgnored(i, libraryFile.Item.Value.Path)))
                {
                    handled++;
                }
                continue;
            }

            var destinationPath = Destination.Path / libraryFile.Item.Path.RelativeTo(found.Path); 
            _ = new LoadoutFile.New(transaction, out var id)
            {
                LoadoutItemWithTargetPath = new LoadoutItemWithTargetPath.New(transaction, id)
                {
                    TargetPath = (loadout.Id, rootDestination.LocationId, destinationPath),
                    LoadoutItem = new LoadoutItem.New(transaction, id)
                    {
                        Name = libraryFile.Item.Path.Name,
                        LoadoutId = loadout.Id,
                        ParentId = loadoutGroup.Id,
                    },
                },
                Hash = libraryFile.Item.LibraryFile.Value.Hash,
                Size = libraryFile.Item.LibraryFile.Value.Size,
            };
            handled++;
        }
        
        if (handled != total)
            return ValueTask.FromResult<InstallerResult>(new NotSupported(Reason: "Did not handle all files"));

        return ValueTask.FromResult<InstallerResult>(new Success());
    }

    private bool IsIgnored(object pattern, RelativePath path)
    {
        return pattern switch
        {
            string s => s.Equals(path, StringComparison.InvariantCultureIgnoreCase),
            Regex r => r.IsMatch(path),
            Extension e => path.Extension == e,
            _ => throw new Exception($"No handler for pattern type {pattern.GetType()}")
        };
    }
}
