using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Input;
using DynamicData;
using DynamicData.Aggregation;
using NexusMods.App.UI.Extensions;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.Interprocess.Jobs;
using NexusMods.DataModel.Loadouts;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.LeftMenu.Items;

public class LaunchButtonViewModel : AViewModel<ILaunchButtonViewModel>, ILaunchButtonViewModel
{
    [Reactive]
    public IGame Game { get; set; } = GameInstallation.Empty.Game;

    [Reactive] public ReactiveCommand<Unit, Unit> Command { get; set; } = Initializers.ReactiveCommandUnitUnit;

    [Reactive] public string Label { get; set; } = "Launch";

    public LaunchButtonViewModel(IInterprocessJobManager manager, LoadoutRegistry loadoutRegistry)
    {
        this.WhenActivated(d =>
        {
            var gameFilter = this.WhenAnyValue(vm => vm.Game)
                .Select(game => (Func<Loadout, bool>) (loadout => loadout.Installation.Game.Domain == game.Domain));
            var loadOuts = loadoutRegistry.Loadouts
                .Filter(gameFilter);

            var validJobs = manager.Jobs
                .Filter(job => job.JobType == JobType.ManageGame);

            var joined = loadOuts.LeftJoin(validJobs, job => job.LoadoutId,
                (loadout, job) => (loadout, RunningJob : job.HasValue ));

            var canExecute = joined
                .Filter(row => !row.RunningJob)
                .IsNotEmpty()
                .OnUI();

            canExecute.Select(exe => exe ? "Launch" : "Analyzing Files")
                .BindTo(this, vm => vm.Label)
                .DisposeWith(d);

            Command = ReactiveCommand.Create(() => {}, canExecute);
        });

    }
}
