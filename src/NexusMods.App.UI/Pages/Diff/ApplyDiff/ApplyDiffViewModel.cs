using NexusMods.Abstractions.Loadouts;
using NexusMods.App.UI.Controls.Trees;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;

namespace NexusMods.App.UI.Pages.Diff.ApplyDiff;

public class ApplyDiffViewModel : APageViewModel<IApplyDiffViewModel>, IApplyDiffViewModel
{
    public ApplyDiffViewModel(IWindowManager windowManager) : base(windowManager)
    {
        
    }

    public LoadoutId LoadoutId { get; set; }
    public IFileTreeViewModel? FileTreeViewModel { get; }
}
