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

    public FooterStepperDesignViewModel(Percent progress)
    {
        Progress = progress;
    }

    public FooterStepperDesignViewModel()
    {
        this.WhenActivated(disposables =>
        {
            this.WhenAnyValue(x => x.CurrentValue)
                .Select(current => Percent.CreateClamped(current, 10))
                .Subscribe(progress => Progress = progress)
                .DisposeWith(disposables);

            var canGoToNext = this
                .WhenAnyValue(x => x.CurrentValue)
                .Select(x => x < 10);

            GoToNextCommand = ReactiveCommand.Create(() =>
            {
                CurrentValue += 1;
            }, canGoToNext).DisposeWith(disposables);

            var canGoToPrev = this
                .WhenAnyValue(x => x.CurrentValue)
                .Select(x => x > 0);

            GoToPrevCommand = ReactiveCommand.Create(() =>
            {
                CurrentValue -= 1;
            }, canGoToPrev).DisposeWith(disposables);
        });
    }
}
