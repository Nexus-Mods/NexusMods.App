using System.Reactive;
using System.Windows.Input;
using ReactiveUI;

namespace NexusMods.App.UI.LeftMenu.Items;

public interface IApplyControlViewModel : IViewModelInterface
{
    ReactiveCommand<Unit,Unit> ApplyCommand { get; }
    
    ReactiveCommand<Unit,Unit> IngestCommand { get; }
    
    
    ILaunchButtonViewModel LaunchButtonViewModel { get; }
    
    string ApplyButtonText { get; }
}
