using System.Reactive;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.RateLimiting;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.LeftMenu.Items;

public class LaunchButtonDesignViewModel : AViewModel<ILaunchButtonViewModel>, ILaunchButtonViewModel
{
    [Reactive]
    public IGame Game { get; set; } = GameInstallation.Empty.Game;

    [Reactive]
    public ReactiveCommand<Unit, Unit> Command { get; set; }

    [Reactive] public bool IsEnabled { get; set; } = true;

    [Reactive] public bool IsRunning { get; set; }

    [Reactive]
    public string Label { get; set; } = "LAUNCH";

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

            Label = "Launch";
        });
    }
}
