using FluentAssertions;
using NexusMods.Games.AdvancedInstaller.UI.Preview;
using NexusMods.Paths;

namespace NexusMods.Games.AdvancedInstaller.UI.Tests.Helpers;

internal static class ResultsVMTestHelpers
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

    internal static IPreviewTreeEntryViewModel AssertChildNode(IPreviewTreeEntryViewModel parentViewModel, string nodeName)
    {
        var node = parentViewModel.GetNode(nodeName);
        node.Should().NotBeNull($"because {nodeName} should exist");
        node!.IsNew.Should().BeTrue($"because {nodeName} should be marked as new");
        // right now we show 'new' on all elements.
        return node!;
    }
}

internal static class ResultsNodeExtensions
{
    public static IPreviewTreeEntryViewModel? GetNode(this IPreviewTreeEntryViewModel root, string expectedName) =>
        root.Children.FirstOrDefault(x => x.FileName == expectedName);
}
