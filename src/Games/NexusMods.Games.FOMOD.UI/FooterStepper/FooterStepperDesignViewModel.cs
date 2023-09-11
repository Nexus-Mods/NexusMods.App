using System.Reactive.Disposables;
using System.Reactive.Linq;
using NexusMods.App.UI;
using NexusMods.DataModel.RateLimiting;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.Games.FOMOD.UI;

public class FooterStepperDesignViewModel : FooterStepperViewModel
{
    [Reactive] public int CurrentValue { get; set; } = 5;

    public FooterStepperDesignViewModel()
    {
        this.WhenActivated(disposable =>
        {
            this.WhenAnyValue(x => x.CurrentValue)
                .SubscribeWithErrorLogging(logger: default, current =>
                {
                    Progress = Percent.CreateClamped(current, 10);
                })
                .DisposeWith(disposable);

            var canGoToNext = this
                .WhenAnyValue(x => x.CurrentValue)
                .Select(x => x < 10);

            GoToNextCommand = ReactiveCommand.Create(() =>
            {
                CurrentValue += 1;
            }, canGoToNext).DisposeWith(disposable);

            var canGoToPrev = this
                .WhenAnyValue(x => x.CurrentValue)
                .Select(x => x > 0);

            GoToPrevCommand = ReactiveCommand.Create(() =>
            {
                CurrentValue -= 1;
            }, canGoToPrev).DisposeWith(disposable);
        });
    }
}
