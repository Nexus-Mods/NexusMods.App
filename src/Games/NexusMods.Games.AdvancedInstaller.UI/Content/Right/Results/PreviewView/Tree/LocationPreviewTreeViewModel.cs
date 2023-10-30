using NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.PreviewView.PreviewEntry;
using NexusMods.Paths;

namespace NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.PreviewView;

public sealed class LocationPreviewTreeViewModel : LocationPreviewTreeBaseViewModel
{
    private readonly GamePath _gamePath;
    public LocationPreviewTreeViewModel(GamePath originPath) => _gamePath = originPath;
    protected override ITreeEntryViewModel GetTreeData() => TreeEntryViewModel.Create(_gamePath, true);
}
