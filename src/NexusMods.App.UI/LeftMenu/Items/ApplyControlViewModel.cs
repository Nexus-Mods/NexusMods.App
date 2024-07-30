using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Ids;
using NexusMods.App.UI.Controls.Navigation;
using NexusMods.App.UI.Pages.Diff.ApplyDiff;
using NexusMods.App.UI.Resources;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Models;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.LeftMenu.Items;

public class ApplyControlViewModel : AViewModel<IApplyControlViewModel>, IApplyControlViewModel
{
    private readonly IConnection _conn;
    private readonly ISynchronizerService _syncService;

    private readonly LoadoutId _loadoutId;
    private readonly GameInstallation _gameInstallation;
    
    private bool _isFirstLoad = true;

    [Reactive] private Abstractions.Loadouts.Loadout.ReadOnly NewestLoadout { get; set; }
    [Reactive] private LoadoutId LastAppliedLoadoutId { get; set; }
    [Reactive] private LoadoutWithTxId LastAppliedWithTxId { get; set; }
    [Reactive] private bool CanApply { get; set; } = true;

    public ReactiveCommand<Unit, Unit> ApplyCommand { get; }
    public ReactiveCommand<NavigationInformation, Unit> ShowApplyDiffCommand { get; }

    [Reactive] public string ApplyButtonText { get; private set; } = Language.ApplyControlViewModel__APPLY;
    [Reactive] public bool IsLaunchButtonEnabled { get; private set; } = true;

    public ILaunchButtonViewModel LaunchButtonViewModel { get; }

    public ApplyControlViewModel(LoadoutId loadoutId, IServiceProvider serviceProvider)
    {
        _loadoutId = loadoutId;
        _syncService = serviceProvider.GetRequiredService<ISynchronizerService>();
        _conn = serviceProvider.GetRequiredService<IConnection>();
        var windowManager = serviceProvider.GetRequiredService<IWindowManager>();

        LaunchButtonViewModel = serviceProvider.GetRequiredService<ILaunchButtonViewModel>();
        LaunchButtonViewModel.LoadoutId = loadoutId;

        NewestLoadout = Abstractions.Loadouts.Loadout.Load(_conn.Db, _loadoutId);
        if (!NewestLoadout.IsValid()) 
            throw new ArgumentException("Loadout not found: " + loadoutId);

        _gameInstallation = NewestLoadout.InstallationInstance;

        ApplyCommand = ReactiveCommand.CreateFromTask(async () => await Apply(), 
            canExecute: this.WhenAnyValue(vm => vm.CanApply));
        
        ShowApplyDiffCommand = ReactiveCommand.Create<NavigationInformation>(info =>
        {
            var pageData = new PageData
            {
                FactoryId = ApplyDiffPageFactory.StaticId,
                Context = new ApplyDiffPageContext
                {
                    LoadoutId = _loadoutId,
                },
            };

            var workspaceController = windowManager.ActiveWorkspaceController;

            var behavior = workspaceController.GetOpenPageBehavior(pageData, info);
            var workspaceId = workspaceController.ActiveWorkspaceId;
            workspaceController.OpenPage(workspaceId, pageData, behavior);
        });

        this.WhenActivated(disposables =>
            {
                // Newest Loadout
                Abstractions.Loadouts.Loadout.RevisionsWithChildUpdates(_conn, _loadoutId)
                    .OnUI()
                    .BindToVM(this, vm => vm.NewestLoadout)
                    .DisposeWith(disposables);
                
                // Last applied loadoutTxId
                _syncService.LastAppliedRevisionFor(_gameInstallation)
                    .OnUI()
                    .BindToVM(this, vm => vm.LastAppliedWithTxId)
                    .DisposeWith(disposables);
                
                // Last applied LoadoutId
                this.WhenAnyValue(vm => vm.LastAppliedWithTxId)
                    .Select(revId =>
                        {
                            var loadout = Abstractions.Loadouts.Loadout.Load(_conn.AsOf(revId.Tx),revId.Id);
                            if (!loadout.IsValid())
                                throw new ArgumentException("Loadout not found for revision: " + revId);
                            return loadout.LoadoutId;
                        }
                    )
                    .OnUI()
                    .BindToVM(this, vm => vm.LastAppliedLoadoutId)
                    .DisposeWith(disposables);

                // Changes to either newest loadout or last applied loadout
                var loadoutOrLastAppliedTxObservable = this.WhenAnyValue(
                    vm => vm.NewestLoadout,
                    vm => vm.LastAppliedWithTxId
                );

                // CanApply and IsLaunchButtonEnabled
                Observable.CombineLatest(
                    loadoutOrLastAppliedTxObservable,
                    ApplyCommand.IsExecuting,
                    LaunchButtonViewModel.Command.IsExecuting,
                    (loadoutTuple, isApplying, isToolRunning) => (
                        IsApplying: isApplying, 
                        IsToolRunning: isToolRunning, 
                        LastAppliedWithTxId: loadoutTuple.Item2)
                    )
                    .OnUI()
                    .Subscribe(data =>
                        {
                            var (isApplying, isToolRunning, lastAppliedWithTxId) = data;
                            CanApply = !isApplying &&
                                       !isToolRunning &&
                                       !NewestLoadout.GetLoadoutWithTxId().Equals(lastAppliedWithTxId);
                            IsLaunchButtonEnabled = !isApplying && !CanApply;
                        }
                    ).DisposeWith(disposables);
                
                // Perform an ingest on first load:
                Task.Run(FirstLoadIngest);
            }
        );
    }

    private async Task Apply()
    {
        await Task.Run(async () =>
        {
            var loadout = Abstractions.Loadouts.Loadout.Load(_conn.Db, _loadoutId);
            await _syncService.Synchronize(loadout);
        });
    }
    
    private async Task FirstLoadIngest()
    {
        if (_isFirstLoad)
        {
            _isFirstLoad = false;

            if (LastAppliedWithTxId.Id.Equals(_loadoutId))
            {
                var loadout = Abstractions.Loadouts.Loadout.Load(_conn.Db, _loadoutId);
                await _syncService.Synchronize(loadout);
            }
        }
    }
}
