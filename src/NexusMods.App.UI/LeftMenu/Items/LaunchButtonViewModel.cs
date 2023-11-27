using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using DynamicData;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Values;
using NexusMods.App.UI.Resources;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.Interprocess.Jobs;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.Markers;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.LeftMenu.Items;

public class LaunchButtonViewModel : AViewModel<ILaunchButtonViewModel>, ILaunchButtonViewModel
{
    [Reactive] public LoadoutId LoadoutId { get; set; } = Initializers.LoadoutId;

    [Reactive] public ReactiveCommand<Unit, Unit> Command { get; set; } = Initializers.EnabledReactiveCommand;

    [Reactive] public string Label { get; set; } = Language.LaunchButtonViewModel_LaunchGame_LAUNCH;

    [Reactive] public Percent? Progress { get; set; }

    private ReadOnlyObservableCollection<IInterprocessJob> _jobs = new(new ObservableCollection<IInterprocessJob>());

    private readonly LoadoutRegistry _loadoutRegistry;
    private readonly IToolManager _toolManager;

    public LaunchButtonViewModel(ILogger<LaunchButtonViewModel> logger, IToolManager toolManager,
        IInterprocessJobManager manager, LoadoutRegistry loadoutRegistry)
    {
        _toolManager = toolManager;
        _loadoutRegistry = loadoutRegistry;

        this.WhenActivated(d =>
        {
            var lockedLoadouts = manager.Jobs
                .Filter(m => m.Payload is ILoadoutJob);

            var selectedLoadoutFns = this.WhenAnyValue(vm => vm.LoadoutId)
                .Select<LoadoutId, Func<IInterprocessJob, bool>>(loadoutId =>
                    job => loadoutId == ((ILoadoutJob)job.Payload).LoadoutId);

            lockedLoadouts.Filter(selectedLoadoutFns)
                .Bind(out _jobs)
                .SubscribeWithErrorLogging(logger)
                .DisposeWith(d);

            var canExecute = _jobs.WhenAnyValue(coll => coll.Count, count => count == 0);
            Command = ReactiveCommand.CreateFromObservable(() => Observable.StartAsync(LaunchGame, RxApp.TaskpoolScheduler), canExecute.OnUI());
        });
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
