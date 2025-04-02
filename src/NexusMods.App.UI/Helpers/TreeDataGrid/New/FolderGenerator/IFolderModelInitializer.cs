using DynamicData;
using NexusMods.App.UI.Controls;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.App.UI.Helpers.TreeDataGrid.New.FolderGenerator;

/// <summary>
/// Interface for initializing folder models that require additional columns which aggregate information from its children.
/// </summary>
/// <typeparam name="TTreeItemWithPath">The type of tree item with path.</typeparam>
public interface IFolderModelInitializer<TTreeItemWithPath> where TTreeItemWithPath : ITreeItemWithPath
{
    /// <summary>
    /// Initializes the <see cref="CompositeItemModel{EntityId}"/>.
    /// </summary>
    /// <param name="model">The model to initialize.</param>
    /// <param name="recursiveChildFiles">Provides access to all child models, including subfolders.</param>
    static abstract void InitializeModel(CompositeItemModel<EntityId> model, SourceCache<CompositeItemModel<EntityId>, EntityId> recursiveChildFiles);
}

/// <summary>
/// Default implementation of the folder model initializer.
/// Does not perform any additional initialization.
/// </summary>
/// <typeparam name="TTreeItemWithPath">The type of tree item with path.</typeparam>
public class DefaultFolderModelInitializer<TTreeItemWithPath> : IFolderModelInitializer<TTreeItemWithPath>
    where TTreeItemWithPath : ITreeItemWithPath
{
    /// <inheritdoc/>
    public static void InitializeModel(CompositeItemModel<EntityId> model, SourceCache<CompositeItemModel<EntityId>, EntityId> recursiveChildFiles)
    {
        
    }
}
