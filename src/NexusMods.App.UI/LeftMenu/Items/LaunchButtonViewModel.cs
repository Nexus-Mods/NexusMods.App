using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Input;
using DynamicData;
using DynamicData.Aggregation;
using DynamicData.PLinq;
using Microsoft.Extensions.Logging;
using NexusMods.App.UI.Extensions;
using NexusMods.CLI.Verbs;
using NexusMods.DataModel;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.Interprocess.Jobs;
using NexusMods.DataModel.Interprocess.Messages;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.Cursors;
using NexusMods.DataModel.Loadouts.Markers;
using NexusMods.DataModel.RateLimiting;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.LeftMenu.Items;

public class LaunchButtonViewModel : AViewModel<ILaunchButtonViewModel>, ILaunchButtonViewModel
{
    [Reactive]
    public LoadoutId LoadoutId { get; set; } = Initializers.LoadoutId;

    [Reactive] public ReactiveCommand<Unit, Unit> Command { get; set; } = Initializers.ReactiveCommandUnitUnit;

    [Reactive] public string Label { get; set; } = "Launch";

    [Reactive]
    public Percent? Progress { get; set; }

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
                .Select<LoadoutId, Func<IInterprocessJob, bool>>(loadoutId => job => loadoutId == ((ILoadoutJob)job.Payload).LoadoutId);

            lockedLoadouts.Filter(selectedLoadoutFns)
                .Bind(out _jobs)
                .SubscribeWithErrorLogging(logger)
                .DisposeWith(d);

            var canExecute = _jobs.WhenAnyValue(coll => coll.Count, count => count == 0);

            Command = ReactiveCommand.CreateFromTask(LaunchGame, canExecute.OnUI());

        });
    }

    private async Task LaunchGame(CancellationToken token)
    {
        Label = "RUNNING...";
        var marker = new LoadoutMarker(_loadoutRegistry, LoadoutId);
        var tool = _toolManager.GetTools(marker.Value).OfType<IRunGameTool>().First();
        await _toolManager.RunTool(tool, marker.Value, token:token);
        Label = "LAUNCH";
    }
}
