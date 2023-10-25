using NexusMods.DataModel.ModInstallers;
using NexusMods.Paths;
using NexusMods.Paths.FileTree;

namespace NexusMods.Games.AdvancedInstaller.UI.Content.Left;

internal sealed class ModContentViewModel : ModContentDesignViewModel
{
    private readonly FileTreeNode<RelativePath, ModSourceFileEntry> _files;

    public ModContentViewModel(FileTreeNode<RelativePath, ModSourceFileEntry> archiveFiles,
        IModContentUpdateReceiver receiver)
    {
        _files = archiveFiles;
        Receiver = receiver;
    }

    // ReSharper disable once RedundantOverriddenMember
    protected override ITreeEntryViewModel InitTreeData() =>
        TreeEntryViewModel<ModSourceFileEntry>.FromFileTree(_files);
}
