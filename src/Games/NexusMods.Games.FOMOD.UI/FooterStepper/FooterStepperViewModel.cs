using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using NexusMods.App.UI;
using NexusMods.DataModel.RateLimiting;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.Games.FOMOD.UI;

public class FooterStepperViewModel : AViewModel<IFooterStepperViewModel>, IFooterStepperViewModel
{
    [Reactive]
    public bool IsLastStep { get; private set; }

    [Reactive]
    public Percent Progress { get; set; } = Percent.Zero;

    [Reactive]
    public ReactiveCommand<Unit, Unit> GoToNextCommand { get; set; } = Initializers.DisabledReactiveCommand;

    [Reactive]
    public ReactiveCommand<Unit, Unit> GoToPrevCommand { get; set; } = Initializers.DisabledReactiveCommand;

    public FooterStepperViewModel()
    {
        this.WhenActivated(disposables =>
        {
            this.WhenAnyValue(x => x.Progress)
                .Select(progress => progress == Percent.One)
                .SubscribeWithErrorLogging(isLastStep => IsLastStep = isLastStep)
                .DisposeWith(disposables);
        });
    }
}
