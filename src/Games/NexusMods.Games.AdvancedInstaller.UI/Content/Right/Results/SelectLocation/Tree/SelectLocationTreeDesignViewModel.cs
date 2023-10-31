using System.Diagnostics.CodeAnalysis;
using System.Reactive.Subjects;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Controls.Templates;
using NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.SelectLocation.SelectableDirectoryEntry;
using NexusMods.Paths;

namespace NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.SelectLocation;

[ExcludeFromCodeCoverage]
public class SelectLocationTreeDesignViewModel : SelectLocationTreeBaseViewModel
{
    protected override ITreeEntryViewModel GetTreeData() => CreateTestTree();

    private static ITreeEntryViewModel CreateTestTree()
    {
        var fakeObserver = new DummyCoordinator();

        var rootElement = new TreeEntryViewModel(fakeObserver)
        {
            Status = SelectableDirectoryNodeStatus.Regular,
            Path = new GamePath(LocationId.Game, ""),
        };

        var createFolderElement = new TreeEntryViewModel(fakeObserver)
        {
            Status = SelectableDirectoryNodeStatus.Create,
        };

        var dataElement = new TreeEntryViewModel(fakeObserver)
        {
            Status = SelectableDirectoryNodeStatus.Regular,
            Path = new GamePath(LocationId.Game, "Data"),
        };

        var texturesElement = new TreeEntryViewModel(fakeObserver)
        {
            Status = SelectableDirectoryNodeStatus.Regular,
            Path = new GamePath(LocationId.Game, "Data/Textures"),
        };

        var createdElement = new TreeEntryViewModel(fakeObserver)
        {
            Status = SelectableDirectoryNodeStatus.Created,
            Path = new GamePath(LocationId.Game, "Data/Textures/This is a created folder"),
        };

        var editingElement = new TreeEntryViewModel(fakeObserver)
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
    private static void AddChildren(TreeEntryViewModel vm, TreeEntryViewModel[] children)
    {
        foreach (var node in children)
            vm.Children.Add(node);
    }
}
