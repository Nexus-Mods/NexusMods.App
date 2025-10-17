using System.Reactive;
using NexusMods.App.UI.Controls.Navigation;
using NexusMods.UI.Sdk;
using ReactiveUI;

namespace NexusMods.App.UI.LeftMenu.Items;

public interface IApplyControlViewModel : IViewModelInterface
{
    ReactiveCommand<Unit,Unit> ApplyCommand { get; }
    
    ReactiveCommand<NavigationInformation, Unit> ShowApplyDiffCommand { get; }
    
    ILaunchButtonViewModel LaunchButtonViewModel { get; }
    
    bool IsLaunchButtonEnabled { get; }
    
    bool IsProcessing { get; }
    
    bool IsApplying { get; }
    
    string ApplyButtonText { get; }
    
    string ProcessingText { get; }
}
