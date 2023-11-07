using FluentAssertions;
using NexusMods.Games.AdvancedInstaller.UI.ModContent;
using static NexusMods.Games.AdvancedInstaller.UI.Tests.Helpers.ModContentVMTestHelpers;

namespace NexusMods.Games.AdvancedInstaller.UI.Tests.ModContent;

public class NodeSelectionTests
{
    [Fact]
    public void SelectingRootNode_AllChildrenAreSelected()
    {
        var node = CreateTestTreeNode();
        node.BeginSelect();

        foreach (var child in node.ChildrenFlattened())
            child.Status.Should().Be(ModContentNodeStatus.SelectingViaParent);
    }

    [Fact]
    public void DeSelectingRootNode_AllChildrenAreUnselected()
    {
        var node = CreateTestTreeNode();
        node.BeginSelect();
        node.CancelSelect();

        foreach (var child in node.ChildrenFlattened())
            child.Status.Should().Be(ModContentNodeStatus.Default);
    }

    [Fact]
    public void DeSelectingRootNode_RestoresChildStatusCorrectly()
    {
        var node = CreateTestTreeNode();

        // Change the status of a child node.
        var child = GetChildNode(node, "Readme-BWS.txt")!;
        child.SetStatus(ModContentNodeStatus.IncludedExplicit);

        // Select and unselect
        node.BeginSelect();
        node.CancelSelect();

        // Unchanged after selection was cancelled
        child.Status.Should().Be(ModContentNodeStatus.IncludedExplicit);
    }
}
