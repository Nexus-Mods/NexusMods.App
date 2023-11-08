using System.Reactive.Disposables;
using System.Reactive.Linq;
using NexusMods.App.UI;
using NexusMods.DataModel.RateLimiting;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.Games.FOMOD.UI;

public class FooterStepperDesignViewModel : FooterStepperViewModel
{
    [Reactive] private int CurrentValue { get; set; } = 5;

    public FooterStepperDesignViewModel()
    {
        var canGoToNext = this
            .WhenAnyValue(vm => vm.CurrentValue)
            .Select(currentValue => currentValue < 10);

        GoToNextCommand = ReactiveCommand.Create(() => { CurrentValue += 1; }, canGoToNext);

        var canGoToPrev = this
            .WhenAnyValue(vm => vm.CurrentValue)
            .Select(currentValue => currentValue > 0);

        GoToPrevCommand = ReactiveCommand.Create(() => { CurrentValue -= 1; }, canGoToPrev);

        this.WhenActivated(disposables =>
        {
            this.WhenAnyValue(vm => vm.CurrentValue)
                .Select(currentValue => Percent.CreateClamped(currentValue, 10))
                .BindToVM(this, vm => vm.Progress)
                .DisposeWith(disposables);
        });
    }
}
