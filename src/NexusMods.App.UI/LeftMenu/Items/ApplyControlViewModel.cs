using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Serialization.DataModel.Ids;
using NexusMods.App.UI.Resources;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.LeftMenu.Items;

public class ApplyControlViewModel : AViewModel<IApplyControlViewModel>, IApplyControlViewModel
{
    private readonly ILoadoutRegistry _loadoutRegistry;
    private readonly IApplyService _applyService;

    private readonly LoadoutId _loadoutId;
    private readonly GameInstallation _gameInstallation;


    private readonly ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> _applyReactiveCommand;

    [Reactive] private IId LastAppliedRevisionId { get; set; }
    [Reactive] private LoadoutId LastAppliedLoadoutId { get; set; }
    [Reactive] private Abstractions.Loadouts.Loadout NewestLoadout { get; set; }


    public ICommand ApplyCommand => _applyReactiveCommand;

    [Reactive] public bool CanApply { get; private set; }

    [Reactive] public bool IsApplying { get; private set; }

    [Reactive] public string ApplyButtonText { get; private set; } = Language.ApplyControlViewModel__APPLY;

    public ILaunchButtonViewModel LaunchButtonViewModel { get; }


    public ApplyControlViewModel(LoadoutId loadoutId, IServiceProvider serviceProvider)
    {
        _loadoutId = loadoutId;
        _loadoutRegistry = serviceProvider.GetRequiredService<ILoadoutRegistry>();
        _applyService = serviceProvider.GetRequiredService<IApplyService>();
        LaunchButtonViewModel = serviceProvider.GetRequiredService<ILaunchButtonViewModel>();
        LaunchButtonViewModel.LoadoutId = loadoutId;

        NewestLoadout = _loadoutRegistry.Get(_loadoutId) ??
                        throw new ArgumentException("Loadout not found: " + _loadoutId);

        _gameInstallation = NewestLoadout.Installation;

        LastAppliedRevisionId = _applyService.GetLastAppliedLoadout(_gameInstallation) ??
                                throw new ArgumentException("No last applied loadout found for: " +
                                                            _gameInstallation
                                );

        _applyReactiveCommand = ReactiveCommand.CreateFromTask(async () => await Apply());

        this.WhenActivated(disposables =>
            {
                _loadoutRegistry.RevisionsAsLoadouts(loadoutId)
                    .OnUI()
                    .BindToVM(this, vm => vm.NewestLoadout)
                    .DisposeWith(disposables);

                _applyService.LastAppliedRevisionFor(_gameInstallation)
                    .OnUI()
                    .BindToVM(this, vm => vm.LastAppliedRevisionId)
                    .DisposeWith(disposables);

                this.WhenAnyValue(vm => vm.LastAppliedRevisionId)
                    .Select(revId =>
                        {
                            var loadout = _loadoutRegistry.GetLoadout(revId);
                            if (loadout is null)
                                throw new ArgumentException("Loadout not found for revision: " + revId);
                            return loadout.LoadoutId;
                        }
                    )
                    .OnUI()
                    .BindToVM(this, vm => vm.LastAppliedLoadoutId)
                    .DisposeWith(disposables);

                this.WhenAnyValue(vm => vm.NewestLoadout,
                        vm => vm.LastAppliedRevisionId,
                        vm => vm.IsApplying
                    )
                    .Subscribe(_ =>
                        {
                            CanApply = !IsApplying && (
                                !LastAppliedLoadoutId.Equals(_loadoutId) ||
                                !NewestLoadout.DataStoreId.Equals(LastAppliedRevisionId));
                        }
                    )
                    .DisposeWith(disposables);

                _applyReactiveCommand.IsExecuting
                    .Subscribe(isExecuting => IsApplying = isExecuting)
                    .DisposeWith(disposables);

                this.WhenAnyValue(vm => vm.LastAppliedLoadoutId,
                        vm => vm.NewestLoadout
                    )
                    .Select(_ => !LastAppliedLoadoutId.Equals(_loadoutId) ? Language.ApplyControlViewModel__ACTIVATE_AND_APPLY : Language.ApplyControlViewModel__APPLY)
                    .OnUI()
                    .BindToVM(this, vm => vm.ApplyButtonText)
                    .DisposeWith(disposables);
            }
        );
    }

    private async Task Apply()
    {
        await Task.Run(async () => { await _applyService.Apply(_loadoutId); }
        );
    }
}
