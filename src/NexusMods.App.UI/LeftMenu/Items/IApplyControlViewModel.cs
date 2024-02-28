using System.Windows.Input;

namespace NexusMods.App.UI.LeftMenu.Items;

public interface IApplyControlViewModel : IViewModelInterface
{
    ICommand ApplyCommand { get; }

    bool CanApply { get; }

    bool IsApplying { get; }

    ILaunchButtonViewModel LaunchButtonViewModel { get; }
    
    string ApplyButtonText { get; }
}
