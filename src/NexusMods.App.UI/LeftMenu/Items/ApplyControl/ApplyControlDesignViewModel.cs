using System.Reactive;
using NexusMods.Abstractions.UI;
using NexusMods.App.UI.Controls.Navigation;
using NexusMods.App.UI.Resources;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.LeftMenu.Items;

public class ApplyControlDesignViewModel : AViewModel<IApplyControlViewModel>, IApplyControlViewModel
{
    public ReactiveCommand<Unit, Unit> ApplyCommand { get; }
    public ReactiveCommand<Unit, Unit> IngestCommand { get; }

    public ReactiveCommand<NavigationInformation, Unit> ShowApplyDiffCommand { get; }
    [Reactive] public bool CanApply { get; private set; } = true;
    [Reactive] public bool IsApplying { get; private set; } = false;
    public bool IsIngesting { get; private set; }

    public ILaunchButtonViewModel LaunchButtonViewModel { get; } = new LaunchButtonDesignViewModel();
    public bool IsLaunchButtonEnabled { get; } = true;
    public string ApplyButtonText { get; } = Language.ApplyControlViewModel__APPLY;

    public ApplyControlDesignViewModel()
    {
        ShowApplyDiffCommand = ReactiveCommand.Create<NavigationInformation>(_ => { });

        ApplyCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                IsApplying = true;
                CanApply = false;

                await Task.Delay(3000);

                IsApplying = false;
                CanApply = true;
            }
        );

        IngestCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                IsIngesting = true;
                CanApply = false;

                await Task.Delay(3000);

                IsIngesting = false;
                CanApply = true;
            }
        );
    }
}
