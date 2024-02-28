using System.Reactive.Disposables;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Serialization.DataModel.Ids;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.LeftMenu.Items;

public class ApplyControlViewModel : AViewModel<IApplyControlViewModel>, IApplyControlViewModel
{
    private readonly ILoadoutRegistry _loadoutRegistry;
    private readonly IApplyService _applyService;

    private readonly LoadoutId _loadoutId;
    private readonly GameInstallation _gameInstallation;
    private LoadoutId _lastAppliedLoadoutId;
    private IId _lastAppliedRevisionId;
    private readonly ObservableAsPropertyHelper<Abstractions.Loadouts.Loadout> _newestLoadout;

    private Abstractions.Loadouts.Loadout NewestLoadout => _newestLoadout.Value;

    public ICommand ApplyCommand => _applyReactiveCommand;

    private readonly ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> _applyReactiveCommand;

    [Reactive] public bool CanApply { get; private set; }

    [Reactive] public bool IsApplying { get; private set; }

    public ILaunchButtonViewModel LaunchButtonViewModel { get; }


    public ApplyControlViewModel(LoadoutId loadoutId, IServiceProvider serviceProvider)
    {
        _loadoutId = loadoutId;
        _loadoutRegistry = serviceProvider.GetRequiredService<ILoadoutRegistry>();
        _applyService = serviceProvider.GetRequiredService<IApplyService>();
        LaunchButtonViewModel = serviceProvider.GetRequiredService<ILaunchButtonViewModel>();
        LaunchButtonViewModel.LoadoutId = loadoutId;


        _gameInstallation = _loadoutRegistry.Get(_loadoutId)?.Installation ??
                            throw new ArgumentException("Loadout not found: " + _loadoutId);

        _lastAppliedRevisionId = _applyService.GetLastAppliedLoadout(_gameInstallation) ??
                                                         throw new ArgumentException("No last applied loadout found for: " +
                                                                                 _gameInstallation);

        _newestLoadout = _loadoutRegistry.RevisionsAsLoadouts(loadoutId)
            .ToProperty(this, vm => vm.NewestLoadout, scheduler: RxApp.MainThreadScheduler);
        
        _applyReactiveCommand = ReactiveCommand.CreateFromTask(async () => await Apply());

        this.WhenActivated(disposables =>
        {
            this.WhenAnyValue(vm => vm.NewestLoadout,
                    vm => vm._lastAppliedLoadoutId,
                    vm => vm.IsApplying)
                .Subscribe(_ =>
                {
                    CanApply = !IsApplying && (
                        !_lastAppliedLoadoutId.Equals(_loadoutId) ||
                        !NewestLoadout.DataStoreId.Equals(_lastAppliedRevisionId));
                })
                .DisposeWith(disposables);
            
            _applyReactiveCommand.IsExecuting
                .Subscribe(isExecuting => IsApplying = isExecuting)
                .DisposeWith(disposables);
        });
    }

    private async Task Apply()
    {
        _lastAppliedLoadoutId = _loadoutId;
        _lastAppliedRevisionId = _loadoutRegistry.Get(_loadoutId)!.DataStoreId;

        await Task.Run(async () =>
            {
                await _applyService.Apply(_loadoutId);
            }
        );
    }
}
