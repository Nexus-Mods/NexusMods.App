using System.Reactive;
using System.Reactive.Linq;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Jobs;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Exceptions;
using NexusMods.App.UI.Overlays;
using NexusMods.App.UI.Overlays.Generic.MessageBox.Ok;
using NexusMods.App.UI.Resources;
using NexusMods.MnemonicDB.Abstractions;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.LeftMenu.Items;

public class LaunchButtonViewModel : AViewModel<ILaunchButtonViewModel>, ILaunchButtonViewModel
{
    [Reactive] public LoadoutId LoadoutId { get; set; } = Initializers.LoadoutId;

    [Reactive] public ReactiveCommand<Unit, Unit> Command { get; set; }

    public IObservable<bool> IsRunningObservable { get; }

    [Reactive] public string Label { get; set; } = Language.LaunchButtonViewModel_LaunchGame_LAUNCH;

    [Reactive] public Percent? Progress { get; set; }

    private readonly ILogger<ILaunchButtonViewModel> _logger;
    private readonly IToolManager _toolManager;
    private readonly IConnection _conn;
    private readonly IJobMonitor _monitor;
    private readonly IOverlayController _overlayController;

    public LaunchButtonViewModel(ILogger<ILaunchButtonViewModel> logger, IToolManager toolManager, IConnection conn, IJobMonitor monitor, IOverlayController overlayController, GameRunningTracker gameRunningTracker)
    {
        _logger = logger;
        _toolManager = toolManager;
        _conn = conn;
        _monitor = monitor;
        _overlayController = overlayController;

        IsRunningObservable = gameRunningTracker.GetWithCurrentStateAsStarting();
        Command = ReactiveCommand.CreateFromObservable(() => Observable.StartAsync(LaunchGame, RxApp.TaskpoolScheduler));
    }

    private async Task LaunchGame(CancellationToken token)
    {
        Label = Language.LaunchButtonViewModel_LaunchGame_RUNNING;
        try
        {
            var marker = NexusMods.Abstractions.Loadouts.Loadout.Load(_conn.Db, LoadoutId);
            var tool = _toolManager.GetTools(marker).OfType<IRunGameTool>().First();
            await Task.Run(async () =>
            {
                await _toolManager.RunTool(tool, marker, _monitor, token: token);
            }, token);
        }
        catch (ExecutableInUseException)
        {
            await MessageBoxOkViewModel.ShowGameAlreadyRunningError(_overlayController);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error launching game: {ex.Message}\n{ex.StackTrace}");
        }
        Label = Language.LaunchButtonViewModel_LaunchGame_LAUNCH;
    }
}
