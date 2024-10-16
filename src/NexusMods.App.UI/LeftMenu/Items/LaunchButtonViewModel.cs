using System.Reactive;
using System.Reactive.Linq;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Jobs;
using NexusMods.Abstractions.Loadouts;
using NexusMods.App.UI.Resources;
using NexusMods.MnemonicDB.Abstractions;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.LeftMenu.Items;

public class LaunchButtonViewModel : AViewModel<ILaunchButtonViewModel>, ILaunchButtonViewModel
{
    [Reactive] public LoadoutId LoadoutId { get; set; } = Initializers.LoadoutId;

    [Reactive] public ReactiveCommand<Unit, Unit> Command { get; set; }

    [Reactive] public string Label { get; set; } = Language.LaunchButtonViewModel_LaunchGame_LAUNCH;

    [Reactive] public Percent? Progress { get; set; }

    private readonly IToolManager _toolManager;
    private readonly IConnection _conn;

    public LaunchButtonViewModel(ILogger<LaunchButtonViewModel> logger, IToolManager toolManager, IConnection conn)
    {
        _toolManager = toolManager;
        _conn = conn;

        Command = ReactiveCommand.CreateFromObservable(() => Observable.StartAsync(LaunchGame, RxApp.TaskpoolScheduler));
    }

    private async Task LaunchGame(CancellationToken token)
    {
        Label = Language.LaunchButtonViewModel_LaunchGame_RUNNING;
        var marker = NexusMods.Abstractions.Loadouts.Loadout.Load(_conn.Db, LoadoutId);
        var tool = _toolManager.GetTools(marker).OfType<IRunGameTool>().First();
        await Task.Run(async () =>
        {
            await _toolManager.RunTool(tool, marker, token: token);
        }, token);
        Label = Language.LaunchButtonViewModel_LaunchGame_LAUNCH;
    }
}
