using System.Windows.Input;
using NexusMods.App.UI.Resources;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.LeftMenu.Items;

public class ApplyControlDesignViewModel : AViewModel<IApplyControlViewModel>, IApplyControlViewModel
{
    public ICommand ApplyCommand { get; }
    [Reactive] public bool CanApply { get; private set; } = true;
    [Reactive] public bool IsApplying { get; private set; } = false;

    public ILaunchButtonViewModel LaunchButtonViewModel { get; } = new LaunchButtonDesignViewModel();
    public string ApplyButtonText { get; } = Language.ApplyControlViewModel__ACTIVATE_AND_APPLY;

    public ApplyControlDesignViewModel()
    {
        ApplyCommand = ReactiveCommand.CreateFromTask( async () =>
        {
            IsApplying = true;
            CanApply = false;

            await Task.Delay(3000);

            IsApplying = false;
            CanApply = true;
        });
    }
}
