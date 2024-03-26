using System.Windows.Input;
using NexusMods.App.UI.Resources;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.LeftMenu.Items;

public class ApplyControlDesignViewModel : AViewModel<IApplyControlViewModel>, IApplyControlViewModel
{
    public ICommand ApplyCommand { get; }
    public ICommand IngestCommand { get; }
    [Reactive] public bool CanApply { get; private set; } = true;
    public bool CanIngest { get; private set; } = true;
    [Reactive] public bool IsApplying { get; private set; } = false;
    public bool IsIngesting { get; private set; }

    public ILaunchButtonViewModel LaunchButtonViewModel { get; } = new LaunchButtonDesignViewModel();
    public string ApplyButtonText { get; } = Language.ApplyControlViewModel__ACTIVATE_AND_APPLY;

    public ApplyControlDesignViewModel()
    {
        ApplyCommand = ReactiveCommand.CreateFromTask( async () =>
        {
            IsApplying = true;
            CanApply = false;
            CanIngest = false;

            await Task.Delay(3000);

            IsApplying = false;
            CanApply = true;
            CanIngest = true;
        });
        
        IngestCommand = ReactiveCommand.CreateFromTask( async () =>
        {
            IsIngesting = true;
            CanApply = false;
            CanIngest = false;

            await Task.Delay(3000);

            IsIngesting = false;
            CanApply = true;
            CanIngest = true;
        });
    }
}
