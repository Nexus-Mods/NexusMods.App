using System.Diagnostics.CodeAnalysis;
using NexusMods.Paths;

namespace NexusMods.Games.AdvancedInstaller.UI.SelectLocation;

[ExcludeFromCodeCoverage]
public class SelectLocationTreeDesignViewModel : SelectLocationTreeBaseViewModel
{
    protected override ISelectableTreeEntryViewModel GetTreeData() => CreateTestTree();

    private static ISelectableTreeEntryViewModel CreateTestTree()
    {
        var fakeObserver = new DummyCoordinator();

        var rootElement = new SelectableTreeEntryViewModel(fakeObserver)
        {
            Status = SelectableDirectoryNodeStatus.Regular,
            Path = new GamePath(LocationId.Game, ""),
        };

        var createFolderElement = new SelectableTreeEntryViewModel(fakeObserver)
        {
            Status = SelectableDirectoryNodeStatus.Create,
        };

        var dataElement = new SelectableTreeEntryViewModel(fakeObserver)
        {
            Status = SelectableDirectoryNodeStatus.Regular,
            Path = new GamePath(LocationId.Game, "Data"),
        };

        var texturesElement = new SelectableTreeEntryViewModel(fakeObserver)
        {
            Status = SelectableDirectoryNodeStatus.Regular,
            Path = new GamePath(LocationId.Game, "Data/Textures"),
        };

        var createdElement = new SelectableTreeEntryViewModel(fakeObserver)
        {
            Status = SelectableDirectoryNodeStatus.Created,
            Path = new GamePath(LocationId.Game, "Data/Textures/This is a created folder"),
        };

        var editingElement = new SelectableTreeEntryViewModel(fakeObserver)
        {
            Status = SelectableDirectoryNodeStatus.Editing,
        };

        AddChildren(rootElement, new[] { createFolderElement, dataElement });
        AddChildren(dataElement, new[] { createFolderElement, texturesElement });
        AddChildren(texturesElement, new[] { createFolderElement, createdElement, editingElement });
        return rootElement;
    }

    /// <summary>
    /// For testing and preview purposes, don't use for production.
    /// </summary>
    private static void AddChildren(SelectableTreeEntryViewModel vm, SelectableTreeEntryViewModel[] children)
    {
        foreach (var node in children)
            vm.Children.Add(node);
    }
}
