using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.UI;
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
    private readonly ISynchronizerService _syncService;

    private readonly LoadoutId _loadoutId;
    private readonly GameInstallMetadataId _gameMetadataId;
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
        
        _gameMetadataId = NexusMods.Abstractions.Loadouts.Loadout.Load(_conn.Db, loadoutId).InstallationId;

        LaunchButtonViewModel = serviceProvider.GetRequiredService<ILaunchButtonViewModel>();
        LaunchButtonViewModel.LoadoutId = loadoutId;
        
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
                var loadoutStatuses = Observable.FromAsync(() => _syncService.StatusForLoadout(_loadoutId))
                    .Switch();

                var gameStatuses = _syncService.StatusForGame(_gameMetadataId);

                Observable.CombineLatest(loadoutStatuses, gameStatuses, (loadout, game) => (loadout, game))
                    .OnUI()
                    .Subscribe(status =>
                    {
                        var (ldStatus, gameStatus) = status;
                        
                        CanApply = gameStatus != GameSynchronizerState.Busy 
                                   && ldStatus != LoadoutSynchronizerState.Pending 
                                   && ldStatus != LoadoutSynchronizerState.Current;
                        IsLaunchButtonEnabled = ldStatus == LoadoutSynchronizerState.Current && gameStatus != GameSynchronizerState.Busy;
                    })
                    .DisposeWith(disposables);
                
            }
        );
    }

    private async Task Apply()
    {
        await Task.Run(async () =>
        {
            await _syncService.Synchronize(_loadoutId);
        });
    }
}
