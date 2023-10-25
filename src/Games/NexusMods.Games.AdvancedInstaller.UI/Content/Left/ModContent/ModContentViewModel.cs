using NexusMods.DataModel.ModInstallers;
using NexusMods.Paths;
using NexusMods.Paths.FileTree;

namespace NexusMods.Games.AdvancedInstaller.UI.Content.Left;

internal sealed class ModContentViewModel : ModContentBaseViewModel
{
    private readonly ITreeEntryViewModel _vm;

    public ModContentViewModel(FileTreeNode<RelativePath, ModSourceFileEntry> archiveFiles,
        IModContentUpdateReceiver receiver)
    {
        _vm = TreeEntryViewModel<ModSourceFileEntry>.FromFileTree(archiveFiles);
        Receiver = receiver;
    }

    // ReSharper disable once RedundantOverriddenMember
    protected override ITreeEntryViewModel InitTreeData() => _vm;
}
