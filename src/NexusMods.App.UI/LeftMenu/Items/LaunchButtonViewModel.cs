using System.Reactive;
using System.Reactive.Linq;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Activities;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Ids;
using NexusMods.App.UI.Resources;
using NexusMods.MnemonicDB.Abstractions;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.LeftMenu.Items;

public class LaunchButtonViewModel : AViewModel<ILaunchButtonViewModel>, ILaunchButtonViewModel
{
    [Reactive] public LoadoutId LoadoutId { get; set; } = Initializers.LoadoutId;

    [Reactive] public ReactiveCommand<Unit, Unit> Command { get; set; } = Initializers.EnabledReactiveCommand;

    [Reactive] public string Label { get; set; } = Language.LaunchButtonViewModel_LaunchGame_LAUNCH;

    [Reactive] public Percent? Progress { get; set; }

    private readonly IToolManager _toolManager;
    private readonly IConnection _conn;

    public LaunchButtonViewModel(ILogger<LaunchButtonViewModel> logger, IToolManager toolManager,
        IActivityMonitor manager, IConnection conn)
    {
        _toolManager = toolManager;
        _conn = conn;

        Command = ReactiveCommand.CreateFromObservable(() => Observable.StartAsync(LaunchGame, RxApp.TaskpoolScheduler));
    }

    private async Task LaunchGame(CancellationToken token)
    {
        Label = Language.LaunchButtonViewModel_LaunchGame_RUNNING;
        var marker = _conn.Db.Get(LoadoutId);
        var tool = _toolManager.GetTools(marker).OfType<IRunGameTool>().First();
        await Task.Run(async () =>
        {
            await _toolManager.RunTool(tool, marker, token: token);
        }, token);
        Label = Language.LaunchButtonViewModel_LaunchGame_LAUNCH;
    }
}
