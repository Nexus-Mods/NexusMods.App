﻿using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using DynamicData.Kernel;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Ids;
using NexusMods.Abstractions.Serialization.DataModel.Ids;
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
    private readonly IApplyService _applyService;

    private readonly LoadoutId _loadoutId;
    private readonly GameInstallation _gameInstallation;


    private readonly ReactiveCommand<Unit, Unit> _applyReactiveCommand;
    private readonly ReactiveCommand<Unit, Unit> _ingestReactiveCommand;

    private ObservableAsPropertyHelper<LoadoutWithTxId> _lastAppliedRevisionId;
    private LoadoutWithTxId LastAppliedWithTxId => _lastAppliedRevisionId.Value;

    [Reactive] private LoadoutId LastAppliedLoadoutId { get; set; }

    private ObservableAsPropertyHelper<Abstractions.Loadouts.Loadout.Model> _newestLoadout;
    private readonly IConnection _conn;
    private Abstractions.Loadouts.Loadout.Model NewestLoadout => _newestLoadout.Value;


    public ReactiveCommand<Unit, Unit> ApplyCommand => _applyReactiveCommand;
    public ReactiveCommand<Unit, Unit> IngestCommand => _ingestReactiveCommand;
    public ReactiveCommand<NavigationInformation, Unit> ShowApplyDiffCommand { get; }


    [Reactive] private bool CanApply { get; set; } = true;
    [Reactive] private bool CanIngest { get; set; } = true;

    [Reactive] public string ApplyButtonText { get; private set; } = Language.ApplyControlViewModel__APPLY;

    public ILaunchButtonViewModel LaunchButtonViewModel { get; }


    public ApplyControlViewModel(LoadoutId loadoutId, IServiceProvider serviceProvider)
    {
        _loadoutId = loadoutId;
        _applyService = serviceProvider.GetRequiredService<IApplyService>();
        _conn = serviceProvider.GetRequiredService<IConnection>();
        LaunchButtonViewModel = serviceProvider.GetRequiredService<ILaunchButtonViewModel>();
        var windowManager = serviceProvider.GetRequiredService<IWindowManager>();
        LaunchButtonViewModel.LoadoutId = loadoutId;

        var currentLoadout = _conn.Db.Get(loadoutId);
        if (currentLoadout is null)
            throw new ArgumentException("Loadout not found", nameof(loadoutId));

        _newestLoadout = Observable.Return(currentLoadout)
            .Merge(_conn.Revisions(loadoutId))
            .ToProperty(this, vm => vm.NewestLoadout, scheduler: RxApp.MainThreadScheduler);

        _gameInstallation = currentLoadout.Installation;

        _lastAppliedRevisionId = _applyService.LastAppliedRevisionFor(_gameInstallation)
            .ToProperty(this, vm => vm.LastAppliedWithTxId, scheduler: RxApp.MainThreadScheduler);

        _applyReactiveCommand = ReactiveCommand.CreateFromTask(async () => await Apply(), 
            canExecute: this.WhenAnyValue(vm => vm.CanApply));
        _ingestReactiveCommand = ReactiveCommand.CreateFromTask(async () => await Ingest(), 
            canExecute: this.WhenAnyValue(vm => vm.CanIngest));
        
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
                var db = _conn.Db;
                // Last applied loadout id
                this.WhenAnyValue(vm => vm.LastAppliedWithTxId)
                    .Select(revId =>
                        {
                            var loadout = _conn.AsOf(revId.Tx).Get(revId.Id);
                            if (loadout is null)
                                throw new ArgumentException("Loadout not found for revision: " + revId);
                            return loadout.LoadoutId;
                        }
                    )
                    .BindToVM(this, vm => vm.LastAppliedLoadoutId)
                    .DisposeWith(disposables);

                // Apply and Ingest button visibility
                var loadoutOrLastAppliedStream = this.WhenAnyValue(vm => vm.NewestLoadout,
                    vm => vm.LastAppliedWithTxId
                );

                loadoutOrLastAppliedStream.CombineLatest(_applyReactiveCommand.IsExecuting)
                    .CombineLatest(_ingestReactiveCommand.IsExecuting)
                    .Subscribe(data =>
                        {
                            var isApplying = data.First.Second;
                            var isIngesting = data.Second;
                            CanApply = !isApplying && !isIngesting &&
                                       (!LastAppliedLoadoutId.Equals(_loadoutId) ||
                                        !NewestLoadout.LoadoutWithTxId.Equals(LastAppliedWithTxId));
                            CanIngest = !isApplying && !isIngesting &&
                                        LastAppliedLoadoutId.Equals(_loadoutId);
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

    private async Task Ingest()
    {
        await Task.Run(async () => { await _applyService.Ingest(_gameInstallation); });
    }
}
