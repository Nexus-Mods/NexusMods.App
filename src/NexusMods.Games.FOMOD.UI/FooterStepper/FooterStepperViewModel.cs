using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using NexusMods.Abstractions.UI;
using NexusMods.App.UI;
using NexusMods.Sdk.Jobs;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.Games.FOMOD.UI;

public class FooterStepperViewModel : AViewModel<IFooterStepperViewModel>, IFooterStepperViewModel
{
    [Reactive]
    public bool IsLastStep { get; private set; }

    [Reactive]
    public Percent Progress { get; set; } = Percent.Zero;

    [Reactive] public bool CanGoNext { get; set; }
    [Reactive] public bool CanGoPrev { get; set; }
    public ReactiveCommand<Unit, Unit> GoToNextCommand { get; protected init; }
    public ReactiveCommand<Unit, Unit> GoToPrevCommand { get; protected init; }

    public FooterStepperViewModel()
    {
        GoToNextCommand = ReactiveCommand.Create(() => {}, this.WhenAnyValue(vm => vm.CanGoNext));
        GoToPrevCommand = ReactiveCommand.Create(() => {}, this.WhenAnyValue(vm => vm.CanGoPrev));

        this.WhenActivated(disposables =>
        {
            this.WhenAnyValue(vm => vm.Progress)
                .Select(progress => progress == Percent.One)
                .BindToVM(this, vm => vm.IsLastStep)
                .DisposeWith(disposables);
        });
    }
}
