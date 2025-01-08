using System.Reactive;
using System.Reactive.Subjects;
using NexusMods.Abstractions.Jobs;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.UI;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.LeftMenu.Items;

public class LaunchButtonDesignViewModel : AViewModel<ILaunchButtonViewModel>, ILaunchButtonViewModel
{
    [Reactive]
    public LoadoutId LoadoutId { get; set; } = Initializers.LoadoutId;

    [Reactive]
    public ReactiveCommand<Unit, Unit> Command { get; set; }

    public IObservable<bool> IsRunningObservable { get; } = new Subject<bool>();

    [Reactive]
    public string Label { get; set; } = "PLAY";

    [Reactive]
    public Percent? Progress { get; set; }

    public LaunchButtonDesignViewModel()
    {
        Command = ReactiveCommand.CreateFromTask(async () =>
        {
            Label = "PREPARING...";
            Progress = Percent.Zero;
            await Task.Delay(100);
            for (var x = 0; x < 10; x++)
            {

                Progress = Percent.CreateClamped(0.1d + Progress!.Value.Value);
                await Task.Delay(200);
            }

            Label = "RUNNING...";
            Progress = null;
            await Task.Delay(1000);

            Label = "PLAY";
        });
    }
}
