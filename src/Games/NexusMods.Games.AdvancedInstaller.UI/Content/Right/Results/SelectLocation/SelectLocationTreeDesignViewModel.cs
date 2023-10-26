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

        var rootElement = new TreeEntryViewModel
        {
            Status = SelectableDirectoryNodeStatus.Regular,
            Path = new GamePath(LocationId.Game, ""),
            Coordinator = fakeObserver,
        };

        var createFolderElement = new TreeEntryViewModel
        {
            Status = SelectableDirectoryNodeStatus.Create,
            Coordinator = fakeObserver,
        };

        var dataElement = new TreeEntryViewModel
        {
            Status = SelectableDirectoryNodeStatus.Regular,
            Path = new GamePath(LocationId.Game, "Data"),
            Coordinator = fakeObserver,
        };

        var texturesElement = new TreeEntryViewModel
        {
            Status = SelectableDirectoryNodeStatus.Regular,
            Path = new GamePath(LocationId.Game, "Data/Textures"),
            Coordinator = fakeObserver,
        };

        var createdElement = new TreeEntryViewModel
        {
            Status = SelectableDirectoryNodeStatus.Created,
            Path = new GamePath(LocationId.Game, "Data/Textures/This is a created folder"),
            Coordinator = fakeObserver,
        };

        var editingElement = new TreeEntryViewModel
        {
            Status = SelectableDirectoryNodeStatus.Editing,
            Coordinator = fakeObserver,
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
