using System.Diagnostics.CodeAnalysis;
using NexusMods.Paths;
using ITreeEntryViewModel =
    NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.PreviewView.PreviewEntry.ITreeEntryViewModel;
using TreeEntryViewModel =
    NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.PreviewView.PreviewEntry.TreeEntryViewModel;

namespace NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.PreviewView;

[ExcludeFromCodeCoverage]
// ReSharper disable once UnusedType.Global
public class LocationPreviewTreeDesignViewModel : LocationPreviewTreeBaseViewModel
{
    // ReSharper disable once VirtualMemberNeverOverridden.Global
    protected override ITreeEntryViewModel GetTreeData() => CreateTestTree();

    private static ITreeEntryViewModel CreateTestTree()
    {
        var fileEntries = new RelativePath[]
        {
            new("BWS.bsa"),
            new("BWS - Textures.bsa"),
            new("Readme-BWS.txt"),
            new("Textures/greenBlade.dds"),
            new("Textures/greenBlade_n.dds"),
            new("Textures/greenHilt.dds"),
            new("Textures/Armors/greenArmor.dds"),
            new("Textures/Armors/greenBlade.dds"),
            new("Textures/Armors/greenHilt.dds"),
            new("Meshes/greenBlade.nif")
        };

        var target = TreeEntryViewModel.Create(new GamePath(LocationId.Game, ""), true);
        foreach (var file in fileEntries)
            target.AddChildren(file, false);

        return target;
    }
}
