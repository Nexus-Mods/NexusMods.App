using System.Reactive;
using NexusMods.App.UI;
using NexusMods.DataModel.RateLimiting;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.Games.FOMOD.UI;

public class FooterStepperViewModel : AViewModel<IFooterStepperViewModel>, IFooterStepperViewModel
{
    [Reactive]
    public Percent Progress { get; set; } = Percent.Zero;

    [Reactive]
    public ReactiveCommand<Unit, Unit> GoToNextCommand { get; set; } = Initializers.DisabledReactiveCommand;

    [Reactive]
    public ReactiveCommand<Unit, Unit> GoToPrevCommand { get; set; } = Initializers.DisabledReactiveCommand;
}
