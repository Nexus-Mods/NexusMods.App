using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using DynamicData.Kernel;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Diagnostics;
using NexusMods.App.UI.LeftMenu.Items;
using NexusMods.App.UI.Pages.Diagnostics;
using NexusMods.App.UI.Pages.LoadoutGrid;
using NexusMods.App.UI.Resources;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.Icons;
using ReactiveUI;

namespace NexusMods.App.UI.LeftMenu.Loadout;

public class LoadoutLeftMenuViewModel : AViewModel<ILoadoutLeftMenuViewModel>, ILoadoutLeftMenuViewModel
{
    public IApplyControlViewModel ApplyControlViewModel { get; }

    public ReadOnlyObservableCollection<ILeftMenuItemViewModel> Items { get; }
    public WorkspaceId WorkspaceId { get; }

    public LoadoutLeftMenuViewModel(
        LoadoutContext loadoutContext,
        WorkspaceId workspaceId,
        IWorkspaceController workspaceController,
        IServiceProvider serviceProvider)
    {
        var diagnosticManager = serviceProvider.GetRequiredService<IDiagnosticManager>();

        WorkspaceId = workspaceId;
        ApplyControlViewModel = new ApplyControlViewModel(loadoutContext.LoadoutId, serviceProvider);

        var diagnosticItem = new IconViewModel
        {
            Name = Language.LoadoutLeftMenuViewModel_LoadoutLeftMenuViewModel_Diagnostics,
            Icon = IconValues.MonitorDiagnostics,
            Activate = ReactiveCommand.Create(() =>
            {
                var pageData = new PageData
                {
                    FactoryId = DiagnosticListPageFactory.StaticId,
                    Context = new DiagnosticListPageContext
                    {
                        LoadoutId = loadoutContext.LoadoutId,
                    },
                };

                // TODO: use https://github.com/Nexus-Mods/NexusMods.App/issues/942
                var input = NavigationInput.Default;

                var behavior = workspaceController.GetDefaultOpenPageBehavior(pageData, input, Optional<PageIdBundle>.None);
                workspaceController.OpenPage(WorkspaceId, pageData, behavior);
            }),
        };

        var items = new ILeftMenuItemViewModel[]
        {
            new IconViewModel
            {
                Name = Language.LoadoutLeftMenuViewModel_LoadoutGridEntry,
                Icon = IconValues.Collections,
                Activate = ReactiveCommand.Create(() =>
                {
                    var pageData = new PageData
                    {
                        FactoryId = LoadoutGridPageFactory.StaticId,
                        Context = new LoadoutGridContext { LoadoutId = loadoutContext.LoadoutId },
                    };

                    // TODO: use https://github.com/Nexus-Mods/NexusMods.App/issues/942
                    var input = NavigationInput.Default;

                    var behavior = workspaceController.GetDefaultOpenPageBehavior(pageData, input, Optional<PageIdBundle>.None);
                    workspaceController.OpenPage(WorkspaceId, pageData, behavior);
                }),
            },
            diagnosticItem,
        };

        Items = new ReadOnlyObservableCollection<ILeftMenuItemViewModel>(new ObservableCollection<ILeftMenuItemViewModel>(items));

        this.WhenActivated(disposable =>
        {
            diagnosticManager
                .CountDiagnostics(loadoutContext.LoadoutId)
                .OnUI()
                .Subscribe(counts =>
                {
                    var totalCount = counts.NumSuggestions + counts.NumWarnings + counts.NumCritical;
                    diagnosticItem.Name = $"{Language.LoadoutLeftMenuViewModel_LoadoutLeftMenuViewModel_Diagnostics} ({totalCount})";
                })
                .DisposeWith(disposable);
        });
    }
}
