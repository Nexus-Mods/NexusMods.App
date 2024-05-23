using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using DynamicData.Kernel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Ids;
using NexusMods.App.UI.Controls.Navigation;
using NexusMods.App.UI.Pages.Diff.ApplyDiff;
using NexusMods.App.UI.Resources;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.MnemonicDB.Abstractions;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.LeftMenu.Items;

public class ApplyControlViewModel : AViewModel<IApplyControlViewModel>, IApplyControlViewModel
{
    private readonly IConnection _conn;
    private readonly IApplyService _applyService;

    private readonly LoadoutId _loadoutId;
    private readonly GameInstallation _gameInstallation;

    [Reactive] private Abstractions.Loadouts.Loadout.Model NewestLoadout { get; set; }
    [Reactive] private LoadoutId LastAppliedLoadoutId { get; set; }
    [Reactive] private LoadoutWithTxId LastAppliedWithTxId { get; set; }
    
    [Reactive] private bool CanApply { get; set; } = true;

    public ReactiveCommand<Unit, Unit> ApplyCommand { get; }
    public ReactiveCommand<NavigationInformation, Unit> ShowApplyDiffCommand { get; }

    [Reactive] public string ApplyButtonText { get; private set; } = Language.ApplyControlViewModel__APPLY;

    public ILaunchButtonViewModel LaunchButtonViewModel { get; }

    public ApplyControlViewModel(LoadoutId loadoutId, IServiceProvider serviceProvider)
    {
        _loadoutId = loadoutId;
        _applyService = serviceProvider.GetRequiredService<IApplyService>();
        _conn = serviceProvider.GetRequiredService<IConnection>();
        var windowManager = serviceProvider.GetRequiredService<IWindowManager>();

        LaunchButtonViewModel = serviceProvider.GetRequiredService<ILaunchButtonViewModel>();
        LaunchButtonViewModel.LoadoutId = loadoutId;

        NewestLoadout = _conn.Db.Get(loadoutId) ?? throw new ArgumentException("Loadout not found: " + loadoutId);

        _gameInstallation = NewestLoadout.Installation;

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

            if (!windowManager.TryGetActiveWindow(out var activeWindow)) return;
            var workspaceController = activeWindow.WorkspaceController;

            var behavior = workspaceController.GetOpenPageBehavior(pageData, info, Optional<PageIdBundle>.None);
            var workspaceId = workspaceController.ActiveWorkspace!.Id;
            workspaceController.OpenPage(workspaceId, pageData, behavior);
        });

        this.WhenActivated(disposables =>
            {
                // Newest Loadout
                _conn.Revisions(loadoutId)
                    .OnUI()
                    .BindToVM(this, vm => vm.NewestLoadout)
                    .DisposeWith(disposables);
                
                // Last applied loadoutTxId
                _applyService.LastAppliedRevisionFor(_gameInstallation)
                    .OnUI()
                    .BindToVM(this, vm => vm.LastAppliedWithTxId)
                    .DisposeWith(disposables);
                
                // Last applied LoadoutId
                this.WhenAnyValue(vm => vm.LastAppliedWithTxId)
                    .Select(revId =>
                        {
                            var loadout = _conn.AsOf(revId.Tx).Get(revId.Id);
                            if (loadout is null)
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

                // CanApply
                loadoutOrLastAppliedTxObservable.CombineLatest(ApplyCommand.IsExecuting)
                    .OnUI()
                    .Subscribe(data =>
                        {
                            var isApplying = data.Second;
                            var lastAppliedWithTxId = data.First.Item2;
                            CanApply = !isApplying && 
                                       !NewestLoadout.GetLoadoutWithTxId().Equals(lastAppliedWithTxId);
                        }
                    ).DisposeWith(disposables);
                
                // Apply button text
                this.WhenAnyValue(vm => vm.LastAppliedLoadoutId,
                        vm => vm.NewestLoadout
                    )
                    .Select(_ =>
                        !LastAppliedLoadoutId.Equals(_loadoutId)
                            ? Language.ApplyControlViewModel__ACTIVATE_AND_APPLY
                            : Language.ApplyControlViewModel__APPLY
                    )
                    .OnUI()
                    .BindToVM(this, vm => vm.ApplyButtonText)
                    .DisposeWith(disposables);
            }
        );
    }

    private async Task Apply()
    {
        await Task.Run(async () =>
        {
            var loadout = _conn.Db.Get(_loadoutId);
            await _applyService.Apply(loadout);
        });
    }
    
}
