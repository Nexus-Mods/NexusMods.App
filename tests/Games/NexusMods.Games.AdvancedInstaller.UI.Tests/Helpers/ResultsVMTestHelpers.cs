using FluentAssertions;
using NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.PreviewView.PreviewEntry;
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

    internal static ITreeEntryViewModel AssertChildNode(ITreeEntryViewModel parentViewModel, string nodeName)
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
    public static ITreeEntryViewModel? GetNode(this ITreeEntryViewModel root, string expectedName) =>
        root.Children.FirstOrDefault(x => x.FileName == expectedName);
}
