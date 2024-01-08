using System.Reactive;
using System.Reactive.Linq;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Activities;
using NexusMods.Abstractions.Values;
using NexusMods.App.UI.Resources;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.Loadouts;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.LeftMenu.Items;

public class LaunchButtonViewModel : AViewModel<ILaunchButtonViewModel>, ILaunchButtonViewModel
{
    [Reactive] public LoadoutId LoadoutId { get; set; } = Initializers.LoadoutId;

    [Reactive] public ReactiveCommand<Unit, Unit> Command { get; set; } = Initializers.EnabledReactiveCommand;

    [Reactive] public string Label { get; set; } = Language.LaunchButtonViewModel_LaunchGame_LAUNCH;

    [Reactive] public Percent? Progress { get; set; }
    
    private readonly LoadoutRegistry _loadoutRegistry;
    private readonly IToolManager _toolManager;

    public LaunchButtonViewModel(ILogger<LaunchButtonViewModel> logger, IToolManager toolManager,
        IActivityMonitor manager, LoadoutRegistry loadoutRegistry)
    {
        _toolManager = toolManager;
        _loadoutRegistry = loadoutRegistry;

        Command = ReactiveCommand.CreateFromObservable(() => Observable.StartAsync(LaunchGame, RxApp.TaskpoolScheduler));
    }

    private async Task LaunchGame(CancellationToken token)
    {
        Label = Language.LaunchButtonViewModel_LaunchGame_RUNNING;
        var marker = _loadoutRegistry.GetMarker(LoadoutId);
        var tool = _toolManager.GetTools(marker.Value).OfType<IRunGameTool>().First();
        await Task.Run(async () =>
        {
            await _toolManager.RunTool(tool, marker.Value, token: token);
        }, token);
        Label = Language.LaunchButtonViewModel_LaunchGame_LAUNCH;
    }
}
