using NexusMods.Paths;

namespace NexusMods.Games.AdvancedInstaller.UI.Preview;

public sealed class LocationPreviewTreeViewModel : LocationPreviewTreeBaseViewModel
{
    private readonly GamePath _gamePath;
    public LocationPreviewTreeViewModel(GamePath originPath) => _gamePath = originPath;
    protected override IPreviewTreeEntryViewModel GetTreeData() => PreviewTreeEntryViewModel.Create(_gamePath, true);
}
