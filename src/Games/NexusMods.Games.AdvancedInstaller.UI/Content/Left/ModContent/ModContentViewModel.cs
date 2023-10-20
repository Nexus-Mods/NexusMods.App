using NexusMods.DataModel.ModInstallers;
using NexusMods.Paths;
using NexusMods.Paths.FileTree;

namespace NexusMods.Games.AdvancedInstaller.UI.Content.Left;

internal class ModContentViewModel : ModContentDesignViewModel
{
    private readonly FileTreeNode<RelativePath, ModSourceFileEntry> _files;
    public ModContentViewModel(FileTreeNode<RelativePath, ModSourceFileEntry> archiveFiles) => _files = archiveFiles;

    // TODO: Implement the actual tree data.
    // ReSharper disable once RedundantOverriddenMember
    protected override ITreeEntryViewModel InitTreeData() => new TreeEntryViewModel(ModContentNode<ModSourceFileEntry>.FromFileTree(_files));
}
