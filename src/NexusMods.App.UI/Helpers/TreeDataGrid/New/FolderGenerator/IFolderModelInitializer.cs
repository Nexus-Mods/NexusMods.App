using NexusMods.App.UI.Controls;
using NexusMods.Sdk.Games;

namespace NexusMods.App.UI.Helpers.TreeDataGrid.New.FolderGenerator;

/// <summary>
/// Interface for initializing folder models that require additional columns which aggregate information from its children.
/// </summary>
/// <typeparam name="TTreeItemWithPath">The type of tree item with path.</typeparam>
public interface IFolderModelInitializer<TTreeItemWithPath> where TTreeItemWithPath : ITreeItemWithPath
{
    /// <summary>
    /// Initializes the <see cref="CompositeItemModel{GamePath}"/>.
    /// </summary>
    /// <param name="model">The model to initialize.</param>
    /// <param name="folder">The folder containing this model.</param>
    static abstract void InitializeModel<TFolderModelInitializer>(
        CompositeItemModel<GamePath> model, 
        GeneratedFolder<TTreeItemWithPath, TFolderModelInitializer> folder)
        where TFolderModelInitializer : IFolderModelInitializer<TTreeItemWithPath>;
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
    public static void InitializeModel<TFolderModelInitializer>(
        CompositeItemModel<GamePath> model, 
        GeneratedFolder<TTreeItemWithPath, TFolderModelInitializer> folder)
        where TFolderModelInitializer : IFolderModelInitializer<TTreeItemWithPath>
    {
        
    }
}
