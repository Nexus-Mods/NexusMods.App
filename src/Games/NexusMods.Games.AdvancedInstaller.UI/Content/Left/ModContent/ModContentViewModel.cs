using NexusMods.DataModel.ModInstallers;
using NexusMods.Paths;
using NexusMods.Paths.FileTree;

namespace NexusMods.Games.AdvancedInstaller.UI.ModContent;

internal sealed class ModContentViewModel : ModContentBaseViewModel
{
    private readonly IModContentTreeEntryViewModel _vm;

    public ModContentViewModel(FileTreeNode<RelativePath, ModSourceFileEntry> archiveFiles, IAdvancedInstallerCoordinator coordinator)
    {
        _vm = ModContentTreeEntryViewModel<ModSourceFileEntry>.FromFileTree(archiveFiles, coordinator);
    }

    // ReSharper disable once RedundantOverriddenMember
    protected override IModContentTreeEntryViewModel InitTreeData() => _vm;
}
