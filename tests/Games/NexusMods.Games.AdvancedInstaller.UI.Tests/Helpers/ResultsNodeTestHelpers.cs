using FluentAssertions;
using NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.PreviewView.PreviewEntry;
using NexusMods.Paths;

namespace NexusMods.Games.AdvancedInstaller.UI.Tests.Helpers;

internal static class ResultsNodeTestHelpers
{
    internal static RelativePath[] GetPaths()
    {
        return new[]
        {
            new RelativePath("BWS.bsa"),
            new RelativePath("BWS - Textures.bsa"),
            new RelativePath("Readme-BWS.txt"),
            new RelativePath("Textures/greenBlade.dds"),
            new RelativePath("Textures/greenBlade_n.dds"),
            new RelativePath("Textures/greenHilt.dds"),
            new RelativePath("Textures/Armors/greenArmor.dds"),
            new RelativePath("Textures/Armors/greenBlade.dds"),
            new RelativePath("Textures/Armors/greenHilt.dds"),
            new RelativePath("Meshes/greenBlade.nif")
        };
    }

    internal static IPreviewEntryNode AssertChildNode(IPreviewEntryNode parentNode, string nodeName)
    {
        var node = parentNode.GetNode(nodeName);
        node.Should().NotBeNull($"because {nodeName} should exist");
        node!.IsNew.Should().BeTrue($"because {nodeName} should be marked as new");
        // right now we show 'new' on all elements.
        return node!;
    }
}

internal static class ResultsNodeExtensions
{
    public static IPreviewEntryNode? GetNode(this IPreviewEntryNode root, string expectedName) =>
        root.Children.FirstOrDefault(x => x.Node.AsT2.FileName == expectedName)?.Node.AsT2;
}
