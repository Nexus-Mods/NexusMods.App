using DynamicData;
using NexusMods.App.UI.Controls;
using NexusMods.MnemonicDB.Abstractions;
using R3;

namespace NexusMods.App.UI.Helpers.TreeDataGrid.New.FolderGenerator.Helpers;

/// <summary>
/// Adapter that processes changesets of tree items and passes them to a <see cref="TreeFolderGenerator{TTreeItemWithPath,TFolderModelInitializer}"/>.
/// For convenience, consider wrapping for concrete types like in <see cref="TreeFolderGeneratorLoadoutTreeItemAdapter{TFolderModelInitializer}"/>.
/// </summary>
/// <typeparam name="TKey">Type of key used for the changesets.</typeparam>
/// <typeparam name="TFolderModelInitializer">Type used for initializing folder models, like <see cref="DefaultFolderModelInitializer{TTreeItemWithPath}"/>.</typeparam>
/// <typeparam name="TTreeItemWithPath">Type of tree item that can provide a path. e.g. <see cref="LoadoutItemTreeItemWithPath"/>.</typeparam>
/// <typeparam name="TTreeItemWithPathFactory">A factory that creates <typeparamref name="TTreeItemWithPath"/></typeparam>
/// <remarks>
///     Subscription is dropped when this item is GC'd.
///     For an integration example, look at "ViewLoadoutGroupFilesTreeDataGridAdapter". 
/// </remarks>
public class TreeFolderGeneratorCompositeItemModelAdapter<TTreeItemWithPath, TTreeItemWithPathFactory, TKey, TFolderModelInitializer> : IDisposable
    where TTreeItemWithPath : ITreeItemWithPath
    where TTreeItemWithPathFactory : ITreeItemWithPathFactory<EntityId, TTreeItemWithPath>
    where TKey : notnull
    where TFolderModelInitializer : IFolderModelInitializer<TTreeItemWithPath>
{
    /// <summary>
    /// The under the hood generated 'folder generator' for this adapter.
    /// </summary>
    public readonly TreeFolderGenerator<TTreeItemWithPath, TFolderModelInitializer> FolderGenerator;
    private readonly TTreeItemWithPathFactory _factory;

    /// <summary>
    /// Creates a new instance of <see cref="TreeFolderGeneratorCompositeItemModelAdapter{TTreeItemWithPath,TTreeItemWithPathFactory,TKey,TFolderModelInitializer}"/>.
    /// </summary>
    /// <param name="factory">Factory that creates <typeparamref name="TTreeItemWithPath"/> instances.</param>
    /// <param name="changes">Observable of changes to automatically process.</param>
    public TreeFolderGeneratorCompositeItemModelAdapter(
        TTreeItemWithPathFactory factory,
        IObservable<IChangeSet<CompositeItemModel<EntityId>, EntityId>> changes)
    {
        _factory = factory;
        FolderGenerator = new TreeFolderGenerator<TTreeItemWithPath, TFolderModelInitializer>();
        
        // Subscribe to the changes and pipe them into Adapt
        changes.Subscribe(Adapt);
    }

    /// <summary>
    /// Adapts changes from a changeset by calling the appropriate methods on the folder generator.
    /// </summary>
    /// <param name="changes">The changeset containing changes to process.</param>
    private void Adapt(IChangeSet<CompositeItemModel<EntityId>, EntityId> changes)
    {
        foreach (var change in changes)
        {
            switch (change.Reason)
            {
                case ChangeReason.Add:
                case ChangeReason.Update:
                    FolderGenerator.OnReceiveFile(_factory.CreateItem(change.Current.Key), change.Current);
                    break;
                case ChangeReason.Remove:
                    FolderGenerator.OnDeleteFile(_factory.CreateItem(change.Current.Key), change.Current);
                    break;
                case ChangeReason.Refresh:
                    // Refresh can be treated as an update
                    FolderGenerator.OnReceiveFile(_factory.CreateItem(change.Current.Key), change.Current);
                    break;
                case ChangeReason.Moved:
                    // Note(sewer): This case is not tested, I don't know how to trigger
                    // this event.
                    FolderGenerator.OnDeleteFile(_factory.CreateItem(change.Current.Key), change.Current);
                    FolderGenerator.OnReceiveFile(_factory.CreateItem(change.Current.Key), change.Current);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(change.Reason), change.Reason, @"Unhandled change reason");
            }
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        FolderGenerator.Dispose();
        GC.SuppressFinalize(this);
    }
}
