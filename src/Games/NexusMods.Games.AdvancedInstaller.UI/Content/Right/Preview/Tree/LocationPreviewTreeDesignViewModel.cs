using System.Diagnostics.CodeAnalysis;
using NexusMods.Paths;

namespace NexusMods.Games.AdvancedInstaller.UI.Preview;

[ExcludeFromCodeCoverage]
// ReSharper disable once UnusedType.Global
public class LocationPreviewTreeDesignViewModel : LocationPreviewTreeBaseViewModel
{
    // ReSharper disable once VirtualMemberNeverOverridden.Global
    protected override IPreviewTreeEntryViewModel GetTreeData() => CreateTestTree();

    private static IPreviewTreeEntryViewModel CreateTestTree()
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

        var target = PreviewTreeEntryViewModel.Create(new GamePath(LocationId.Game, ""), true);
        foreach (var file in fileEntries)
            target.AddChildren(file, false);

        return target;
    }
}
