using NexusMods.DataModel.ModInstallers;
using NexusMods.Paths;
using NexusMods.Paths.FileTree;

namespace NexusMods.Games.AdvancedInstaller.UI.Content.Left;

internal sealed class ModContentViewModel : ModContentBaseViewModel
{
    private readonly ITreeEntryViewModel _vm;

    public ModContentViewModel(FileTreeNode<RelativePath, ModSourceFileEntry> archiveFiles, IAdvancedInstallerCoordinator coordinator)
    {
        _vm = TreeEntryViewModel<ModSourceFileEntry>.FromFileTree(archiveFiles, coordinator);
    }

    // ReSharper disable once RedundantOverriddenMember
    protected override ITreeEntryViewModel InitTreeData() => _vm;
}
