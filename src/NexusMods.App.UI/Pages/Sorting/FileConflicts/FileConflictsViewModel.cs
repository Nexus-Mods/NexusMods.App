using System.Collections.Frozen;
using System.Diagnostics;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Media.Imaging;
using DynamicData;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Synchronizers;
using NexusMods.Abstractions.NexusModsLibrary;
using NexusMods.Abstractions.UI;
using NexusMods.App.UI.Controls;
using NexusMods.App.UI.Controls.MarkdownRenderer;
using NexusMods.App.UI.Dialog;
using NexusMods.App.UI.Dialog.Enums;
using NexusMods.App.UI.Extensions;
using NexusMods.App.UI.Windows;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Sdk.Resources;
using NexusMods.UI.Sdk.Dialog;
using R3;
using ReactiveUI;
using ReactiveCommand = R3.ReactiveCommand;

namespace NexusMods.App.UI.Pages.Sorting;

public class FileConflictsViewModel : AViewModel<IFileConflictsViewModel>, IFileConflictsViewModel
{
    public FileConflictsTreeDataGridAdapter TreeDataGridAdapter { get; }

    public FileConflictsViewModel(IServiceProvider serviceProvider, IWindowManager windowManager, LoadoutId loadoutId)
    {
        var connection = serviceProvider.GetRequiredService<IConnection>();

        var loadout = Loadout.Load(connection.Db, loadoutId);
        Debug.Assert(loadout.IsValid());

        var synchronizer = loadout.InstallationInstance.GetGame().Synchronizer;
        TreeDataGridAdapter = new FileConflictsTreeDataGridAdapter(serviceProvider, connection, synchronizer, loadoutId);

        this.WhenActivated(disposables =>
        {
            TreeDataGridAdapter.Activate().AddTo(disposables);

            TreeDataGridAdapter.MessageSubject.Subscribe(msg =>
            {
                var markdownRendererViewModel = serviceProvider.GetRequiredService<IMarkdownRendererViewModel>();
                markdownRendererViewModel.Contents = msg.Markdown;

                _ = windowManager.ShowDialog(DialogFactory.CreateStandardDialog(title: $"Conflicts for {msg.Group.AsLoadoutItem().Name}", new StandardDialogParameters
                {
                    Markdown = markdownRendererViewModel,
                }, buttonDefinitions: [DialogStandardButtons.Close]), DialogWindowType.Modeless);
            }).AddTo(disposables);
        });
    }
}

public record ViewConflictsMessage(LoadoutItemGroup.ReadOnly Group, string Markdown);

public class FileConflictsTreeDataGridAdapter : TreeDataGridAdapter<CompositeItemModel<EntityId>, EntityId>, ITreeDataGirdMessageAdapter<ViewConflictsMessage>
{
    private readonly IConnection _connection;
    private readonly ILoadoutSynchronizer _synchronizer;
    private readonly IResourceLoader<EntityId, Bitmap> _modPageThumbnailPipeline;
    private readonly LoadoutId _loadoutId;

    public Subject<ViewConflictsMessage> MessageSubject { get; } = new();

    public FileConflictsTreeDataGridAdapter(IServiceProvider serviceProvider, IConnection connection, ILoadoutSynchronizer synchronizer, LoadoutId loadoutId)
    {
        _connection = connection;
        _synchronizer = synchronizer;
        _loadoutId = loadoutId;

        _modPageThumbnailPipeline = ImagePipelines.GetModPageThumbnailPipeline(serviceProvider);
    }

    protected override void BeforeModelActivationHook(CompositeItemModel<EntityId> model)
    {
        base.BeforeModelActivationHook(model);

        model.SubscribeToComponentAndTrack<FileConflictsComponents.ViewAction, FileConflictsTreeDataGridAdapter>(
            key: FileConflictsColumns.Actions.ViewComponentKey,
            state: this,
            factory: static (self, itemModel, component) => component.CommandViewConflicts.Subscribe((self, itemModel, component), static (_, state) =>
            {
                var (self, _, component) = state;
                var markdown = component.CreateMarkdown();

                self.MessageSubject.OnNext(new ViewConflictsMessage(component.Group, markdown));
            })
        );
    }

    protected override IObservable<IChangeSet<CompositeItemModel<EntityId>, EntityId>> GetRootsObservable(bool viewHierarchical)
    {
        var sourceCache = new SourceCache<CompositeItemModel<EntityId>, EntityId>(x => x.Key);

        var loadout = Loadout.Load(_connection.Db, _loadoutId);
        var conflictsByGroup = _synchronizer.GetFileConflictsByParentGroup(loadout);
        var conflictsByPath = _synchronizer.GetFileConflicts(loadout).ToFrozenDictionary();

        var enumerable = conflictsByGroup.Select(kv => ToItemModel(kv, conflictsByPath));
        sourceCache.AddOrUpdate(enumerable);

        return sourceCache.Connect();
    }

    private CompositeItemModel<EntityId> ToItemModel(KeyValuePair<LoadoutItemGroup.ReadOnly, LoadoutFile.ReadOnly[]> kv, FrozenDictionary<GamePath, FileConflictGroup> conflictsByPath)
    {
        var (loadoutGroup, loadoutFiles) = kv;
        var itemModel = new CompositeItemModel<EntityId>(loadoutGroup.Id);

        itemModel.Add(SharedColumns.Name.NameComponentKey, new NameComponent(value: loadoutGroup.AsLoadoutItem().Name));
        ImageComponent? imageComponent = null;

        if (loadoutGroup.TryGetAsLibraryLinkedLoadoutItem(out var libraryLinkedLoadoutItem))
        {
            if (libraryLinkedLoadoutItem.LibraryItem.TryGetAsNexusModsLibraryItem(out var nexusLibraryItem))
            {
                imageComponent = ImageComponent.FromPipeline(_modPageThumbnailPipeline, nexusLibraryItem.ModPageMetadataId, ImagePipelines.ModPageThumbnailFallback);
            }
        }

        imageComponent ??= new ImageComponent(value: ImagePipelines.ModPageThumbnailFallback);
        itemModel.Add(SharedColumns.Name.ImageComponentKey, imageComponent);

        itemModel.Add(FileConflictsColumns.Actions.ViewComponentKey, new FileConflictsComponents.ViewAction(loadoutGroup, loadoutFiles, conflictsByPath));

        return itemModel;
    }

    protected override IColumn<CompositeItemModel<EntityId>>[] CreateColumns(bool viewHierarchical)
    {
        var nameColumn = ColumnCreator.Create<EntityId, SharedColumns.Name>();

        return
        [
            viewHierarchical ? ITreeDataGridItemModel<CompositeItemModel<EntityId>, EntityId>.CreateExpanderColumn(nameColumn) : nameColumn,
            ColumnCreator.Create<EntityId, FileConflictsColumns.Actions>(),
        ];
    }
}

public static class FileConflictsColumns
{
    private const string Prefix = "FileConflicts_";

    public class Actions : ICompositeColumnDefinition<Actions>
    {
        public static int Compare<TKey>(CompositeItemModel<TKey> a, CompositeItemModel<TKey> b) where TKey : notnull
        {
            return a.GetOptional<FileConflictsComponents.ViewAction>(ViewComponentKey).Compare(b.GetOptional<FileConflictsComponents.ViewAction>(ViewComponentKey));
        }

        public const string ColumnTemplateResourceKey = Prefix + "Action";
        public static readonly ComponentKey ViewComponentKey = ComponentKey.From(ColumnTemplateResourceKey + "_" + nameof(Actions) + "_" + "View");

        public static string GetColumnHeader() => "Action";
        public static string GetColumnTemplateResourceKey() => ColumnTemplateResourceKey;
    }
}

public static class FileConflictsComponents
{
    public class ViewAction : IItemModelComponent<ViewAction>, IComparable<ViewAction>
    {
        public LoadoutItemGroup.ReadOnly Group { get; }
        private readonly LoadoutFile.ReadOnly[] _loadoutFiles;
        private readonly FrozenDictionary<GamePath, FileConflictGroup> _conflictsByPath;
        public ReactiveCommand<Unit> CommandViewConflicts { get; } = new ReactiveCommand();

        public ViewAction(LoadoutItemGroup.ReadOnly group, LoadoutFile.ReadOnly[] loadoutFiles, FrozenDictionary<GamePath, FileConflictGroup> conflictsByPath)
        {
            Group = group;
            _loadoutFiles = loadoutFiles;
            _conflictsByPath = conflictsByPath;
        }

        public int CompareTo(ViewAction? other) => 0;

        public string CreateMarkdown()
        {
            var markdown = _loadoutFiles
                .Select(GamePath (x) => x.AsLoadoutItemWithTargetPath().TargetPath)
                .Select(gamePath =>
                {
                    var conflicts = _conflictsByPath[gamePath].Items;
                    var conflictingGroups = conflicts.Where(x => x.File.IsT0).Select(x => x.File.AsT0.AsLoadoutItemWithTargetPath().AsLoadoutItem().Parent).ToArray();
                    return (gamePath, conflictingGroups);
                })
                .Select(tuple =>
                {
                    var (gamePath, conflictingGroups) = tuple;
                    var heading = $"## {gamePath}\n";
                    var body = conflictingGroups.Select(x => $"- {x.AsLoadoutItem().Name}").Aggregate((a, b) => $"{a}\n{b}");

                    return $"{heading}\n{body}";
                })
                .Aggregate((a,b) => $"{a}\n{b}");

            return markdown;
        }
    }
}
