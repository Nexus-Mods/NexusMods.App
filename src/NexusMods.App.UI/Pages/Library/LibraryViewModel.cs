using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;
using DynamicData;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.MnemonicDB.Attributes.Extensions;
using NexusMods.Abstractions.NexusModsLibrary;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Pages.Library;

public class LibraryViewModel : APageViewModel<ILibraryViewModel>, ILibraryViewModel
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConnection _connection;

    [Reactive] public ITreeDataGridSource<LibraryNode> Source { get; set; }

    [Reactive] public bool ViewHierarchical { get; set; } = true;

    private readonly SourceCache<LibraryNode, LibraryNodeId> _sourceCache = new(static node => node.Id);

    public LibraryViewModel(
        IWindowManager windowManager,
        IServiceProvider serviceProvider,
        LoadoutId loadoutId) : base(windowManager)
    {
        _serviceProvider = serviceProvider;
        _connection = _serviceProvider.GetRequiredService<IConnection>();

        var loadout = Loadout.Load(_connection.Db, loadoutId.Value);
        var game = loadout.InstallationInstance.Game;

        var localFileObservable = ObserveLocalFiles();
        var modPageObservable = ObserveModPages(game);

        var stack = new Stack<LibraryNode>();

        localFileObservable
            .MergeChangeSets(modPageObservable)
            .Synchronize(stack)
            .OnItemAdded(node =>
            {
                stack.Push(node);

                while (stack.TryPop(out var current))
                {
                    current.LinkedLoadoutItems.Clear();
                    current.LinkedLoadoutItems.Add(LibraryLinkedLoadoutItem.FindByLibraryItem(_connection.Db, current.Id.Id));

                    _sourceCache.AddOrUpdate(current);
                    foreach (var child in current.Children)
                    {
                        stack.Push(child);
                    }
                }
            })
            .OnItemRemoved(node =>
            {
                stack.Push(node);

                while (stack.TryPop(out var current))
                {
                    _sourceCache.RemoveKey(current.Id);
                    foreach (var child in current.Children)
                    {
                        stack.Push(child);
                    }
                }
            })
            .DisposeMany()
            .Bind(out var nodes)
            .SubscribeWithErrorLogging();

        LibraryLinkedLoadoutItem
            .ObserveAll(_connection)
            .Filter(item => item.AsLoadoutItem().LoadoutId == loadoutId)
            .OnUI()
            .OnItemAdded(item =>
            {
                var lookup = _sourceCache.Lookup(item.LibraryItemId.Value);
                if (!lookup.HasValue) return;

                lookup.Value.LinkedLoadoutItems.Add(item);
            })
            .OnItemRemoved(item =>
            {
                var lookup = _sourceCache.Lookup(item.LibraryItemId.Value);
                if (!lookup.HasValue) return;

                lookup.Value.LinkedLoadoutItems.Remove(item);
            })
            .SubscribeWithErrorLogging();

        Source = CreateSource(nodes, createHierarchicalSource: ViewHierarchical);

        this.WhenActivated(disposables =>
        {
            this.WhenAnyValue(vm => vm.ViewHierarchical)
                .Where(viewHierarchical => viewHierarchical && !Source.IsHierarchical)
                .Select(viewHierarchical => CreateSource(nodes, createHierarchicalSource: viewHierarchical))
                .BindToVM(this, vm => vm.Source)
                .DisposeWith(disposables);
        });
    }

    private IObservable<IChangeSet<LibraryNode>> ObserveModPages(ILocatableGame game)
    {
        var modPageSourceCache = new SourceCache<NexusModsModPageLibraryNode, NexusModsModPageMetadataId>(_ => throw new NotSupportedException());

        var nexusModsLibraryFileObservable = NexusModsLibraryFile.ObserveAll(_connection)
            .OnUI()
            .Filter(file => file.FileMetadata.ModPage.GameDomain == game.Domain)
            .Transform(file => (file, new LibraryNode
            {
                Id = file.Id,
                DateAddedToLibrary = file.GetCreatedAt(),
                Name = file.FileMetadata.Name,
                Size = file.AsDownloadedFile().AsLibraryFile().Size,
                Version = file.FileMetadata.Version,
            }))
            .OnItemAdded(tuple =>
            {
                var (file, node) = tuple;
                if (!ViewHierarchical) return;

                var modPageLookup = modPageSourceCache.Lookup(file.FileMetadata.ModPageId);
                if (modPageLookup.HasValue)
                {
                    modPageLookup.Value.Children.Add(node);
                }
                else
                {
                    var modPage = file.FileMetadata.ModPage;
                    var modPageNode = new NexusModsModPageLibraryNode
                    {
                        Id = modPage.Id,
                        DateAddedToLibrary = file.GetCreatedAt(),
                        Name = modPage.Name,
                    };

                    modPageNode.Children.Add(node);
                    node.ParentId = modPageNode.Id;
                    modPageSourceCache.Edit(updater => updater.AddOrUpdate(modPageNode, modPage.NexusModsModPageMetadataId));
                }
            })
            .OnItemRemoved(tuple =>
            {
                var (file, node) = tuple;
                if (!ViewHierarchical) return;

                var modPageLookup = modPageSourceCache.Lookup(file.FileMetadata.ModPageId);
                if (!modPageLookup.HasValue) return;

                modPageLookup.Value.Children.Remove(node);
            })
            .Transform(tuple => tuple.Item2);

        if (!ViewHierarchical) return nexusModsLibraryFileObservable;
        nexusModsLibraryFileObservable.SubscribeWithErrorLogging();

        return modPageSourceCache.Connect().RemoveKey().Cast(static node => (LibraryNode)node);
    }

    private IObservable<IChangeSet<LibraryNode>> ObserveLocalFiles()
    {
        var localFileObservable = LocalFile
            .ObserveAll(_connection)
            .OnUI()
            .Transform(file =>
            {
                var fileNode = new LibraryNode
                {
                    Id = file.Id,
                    DateAddedToLibrary = file.GetCreatedAt(),
                    Name = file.AsLibraryFile().FileName,
                    Size = file.AsLibraryFile().Size,
                };

                var node = new LibraryNode
                {
                    Id = new LibraryNodeId(prefix: 1, file.Id),
                    DateAddedToLibrary = file.GetCreatedAt(),
                    Name = file.AsLibraryFile().FileName.ReplaceExtension(Extension.None),
                    Size = file.AsLibraryFile().Size,
                };

                node.Children.Add(fileNode);
                fileNode.ParentId = node.Id;
                return node;
            });

        return localFileObservable;
    }

    private static ITreeDataGridSource<LibraryNode> CreateSource(IEnumerable<LibraryNode> nodes, bool createHierarchicalSource)
    {
        if (createHierarchicalSource)
        {
            var source = new HierarchicalTreeDataGridSource<LibraryNode>(nodes);
            AddColumns(source.Columns, viewAsTree: true);
            return source;
        }
        else
        {
            var source = new FlatTreeDataGridSource<LibraryNode>(nodes);
            AddColumns(source.Columns, viewAsTree: false);
            return source;
        }
    }

    private static void AddColumns(ColumnList<LibraryNode> columnList, bool viewAsTree)
    {
        var nameColumn = LibraryNode.CreateNameColumn();

        columnList.Add(viewAsTree ? LibraryNode.CreateExpanderColumn(nameColumn) : nameColumn);
        columnList.Add(LibraryNode.CreateVersionColumn());
        columnList.Add(LibraryNode.CreateSizeColumn());
        columnList.Add(LibraryNode.CreateDateAddedToLibraryColumn());
        columnList.Add(LibraryNode.CreateDateAddedToLoadoutColumn());
    }
}
