using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;
using DynamicData;
using DynamicData.Kernel;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Library;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.MnemonicDB.Attributes.Extensions;
using NexusMods.Abstractions.NexusModsLibrary;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;
using R3;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Observable = R3.Observable;

namespace NexusMods.App.UI.Pages.Library;

public class LibraryViewModel : APageViewModel<ILibraryViewModel>, ILibraryViewModel
{
    private readonly IConnection _connection;
    private readonly ILibraryService _libraryService;

    [Reactive] public ITreeDataGridSource<LibraryNode> Source { get; set; }

    [Reactive] public bool ViewHierarchical { get; set; } = true;

    private readonly SourceCache<LibraryNode, LibraryNodeId> _sourceCache = new(static node => node.Id);
    private readonly ConnectableObservable<DateTime> _ticker;

    public LibraryViewModel(
        IWindowManager windowManager,
        IServiceProvider serviceProvider,
        LoadoutId loadoutId) : base(windowManager)
    {
        _libraryService = serviceProvider.GetRequiredService<ILibraryService>();
        _connection = serviceProvider.GetRequiredService<IConnection>();

        _ticker = Observable
            .Return(DateTime.Now)
            .Merge(Observable.Interval(period: TimeSpan.FromHours(1), timeProvider: ObservableSystem.DefaultTimeProvider).Select(_ => DateTime.Now))
            .Prepend(DateTime.Now)
            .DefaultIfEmpty(DateTime.Now)
            .Publish(initialValue: DateTime.Now);
            // .Replay(bufferSize: 1);

        _ticker.Connect();

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

        _sourceCache
            .Connect()
            .MergeMany(static node => node.AddToLoadoutCommand)
            .ToObservable()
            .Select(_connection, static (node, connection) => node.GetLibraryItemToInstall(connection))
            .Where(libraryItem => libraryItem.IsValid())
            .SubscribeAwait(async (libraryItem, cancellationToken) =>
            {
                await using var job = _libraryService.InstallItem(libraryItem, Loadout.Load(_connection.Db, loadoutId));
                await job.StartAsync(cancellationToken: cancellationToken);
                await job.WaitToFinishAsync(cancellationToken: cancellationToken);
            }, awaitOperation: AwaitOperation.Parallel, cancelOnCompleted: true, configureAwait: false);

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
            .Transform(file => (file, new LibraryNode(_ticker)
            {
                Id = file.Id,
                ParentId = ViewHierarchical ? Optional<LibraryNodeId>.Create(file.FileMetadata.ModPageId.Value) : Optional<LibraryNodeId>.None,
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
                    var modPageNode = new NexusModsModPageLibraryNode(_ticker)
                    {
                        Id = modPage.Id,
                        ParentId = Optional<LibraryNodeId>.None,
                        DateAddedToLibrary = file.GetCreatedAt(),
                        Name = modPage.Name,
                    };

                    modPageNode.Children.Add(node);
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
                var fileNode = new LibraryNode(_ticker)
                {
                    Id = file.Id,
                    ParentId = ViewHierarchical ? new LibraryNodeId(prefix: 1, file.Id) : Optional<LibraryNodeId>.None,
                    DateAddedToLibrary = file.GetCreatedAt(),
                    Name = file.AsLibraryFile().FileName,
                    Size = file.AsLibraryFile().Size,
                };

                var node = new LibraryNode(_ticker)
                {
                    Id = new LibraryNodeId(prefix: 1, file.Id),
                    ParentId = Optional<LibraryNodeId>.None,
                    DateAddedToLibrary = file.GetCreatedAt(),
                    Name = file.AsLibraryFile().FileName.ReplaceExtension(Extension.None),
                    Size = file.AsLibraryFile().Size,
                };

                node.Children.Add(fileNode);
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
        columnList.Add(LibraryNode.CreateAddToLoadoutButtonColumn());
    }
}
