using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Threading;
using DynamicData;
using DynamicData.Kernel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.App.UI.Controls.UnifiedIcon;
using NexusMods.App.UI.Extensions;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceAttachments;
using NexusMods.Extensions.BCL;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.WorkspaceSystem;

internal sealed class WorkspaceController : ReactiveObject, IWorkspaceController
{
    private readonly IWorkspaceWindow _window;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<WorkspaceController> _logger;
    private readonly IWorkspaceAttachmentsFactoryManager _workspaceAttachementsFactory;

    public WindowId WindowId => _window.WindowId;

    private readonly SourceCache<WorkspaceViewModel, WorkspaceId> _workspaces = new(x => x.Id);
    private readonly ReadOnlyObservableCollection<IWorkspaceViewModel> _allWorkspaces;
    public ReadOnlyObservableCollection<IWorkspaceViewModel> AllWorkspaces => _allWorkspaces;

    [Reactive] public IWorkspaceViewModel? ActiveWorkspace { get; private set; }

    public WorkspaceController(IWorkspaceWindow window, IServiceProvider serviceProvider)
    {
        _window = window;

        _serviceProvider = serviceProvider;
        _logger = serviceProvider.GetRequiredService<ILogger<WorkspaceController>>();
        _workspaceAttachementsFactory = serviceProvider.GetRequiredService<IWorkspaceAttachmentsFactoryManager>();

        _workspaces
            .Connect()
            .Transform(x => (IWorkspaceViewModel)x)
            .Bind(out _allWorkspaces)
            .Subscribe();
    }

    public WindowData ToData()
    {
        var workspaces = _allWorkspaces.Select(workspace => workspace.ToData()).ToArray();
        var data = new WindowData
        {
            DataStoreId = WindowData.Id,
            ActiveWorkspaceId = ActiveWorkspace?.Id,
            Workspaces = workspaces,
        };

        return data;
    }

    public void FromData(WindowData data)
    {
        Dispatcher.UIThread.VerifyAccess();
        Debug.Assert(_workspaces.Count == 0);

        IWorkspaceViewModel? activeWorkspace = null;

        foreach (var workspaceData in data.Workspaces)
        {
            var vm = CreateWorkspace(
                context: Optional<IWorkspaceContext>.Create(workspaceData.Context),
                pageData: Optional<PageData>.None
            );

            vm.FromData(workspaceData);

            if (workspaceData.Id == data.ActiveWorkspaceId)
                activeWorkspace = vm;
        }

        ChangeActiveWorkspace(activeWorkspace?.Id ?? _workspaces.Keys.First());
    }

    public IWorkspaceViewModel CreateWorkspace(Optional<IWorkspaceContext> context, Optional<PageData> pageData)
    {
        Dispatcher.UIThread.VerifyAccess();

        var vm = new WorkspaceViewModel(
            workspaceController: this,
            factoryController: _serviceProvider.GetRequiredService<PageFactoryController>(),
            unregisterFunc: UnregisterWorkspace
        )
        {
            Context = context.HasValue ? context.Value : EmptyContext.Instance
        };

        vm.Title = _workspaceAttachementsFactory.CreateTitleFor(vm.Context);

        _workspaces.AddOrUpdate(vm);

        var addPanelBehavior = pageData.HasValue
            ? new AddPanelBehavior(new AddPanelBehavior.WithCustomTab(pageData.Value))
            : new AddPanelBehavior(new AddPanelBehavior.WithDefaultTab());

        vm.AddPanel(
            WorkspaceGridState.From(new[]
            {
                new PanelGridState(PanelId.DefaultValue, MathUtils.One)
            }, isHorizontal: vm.IsHorizontal),
            addPanelBehavior
        );

        return vm;
    }

    private void UnregisterWorkspace(WorkspaceViewModel workspaceViewModel)
    {
        Dispatcher.UIThread.VerifyAccess();

        _workspaces.Remove(workspaceViewModel.Id);

        if (ReferenceEquals(ActiveWorkspace, workspaceViewModel) && AllWorkspaces.Count > 0)
        {
            ChangeActiveWorkspace(AllWorkspaces.First().Id);
        }
    }

    private bool TryGetWorkspace(WorkspaceId workspaceId,
        [NotNullWhen(true)] out WorkspaceViewModel? workspaceViewModel)
    {
        if (!_workspaces.Lookup(workspaceId).TryGet(out workspaceViewModel))
        {
            _logger.LogError("Failed to retrieve the Workspace View Model with the ID {WorkspaceID}", workspaceId);
            return false;
        }

        return true;
    }

    /// <inheritdoc/>
    public bool TryGetWorkspace(WorkspaceId workspaceId, [NotNullWhen(true)] out IWorkspaceViewModel? workspace)
    {
        var res = TryGetWorkspace(workspaceId, out WorkspaceViewModel? tmp);
        workspace = tmp;
        return res;
    }

    public IEnumerable<ValueTuple<IWorkspaceViewModel, TContext>> FindWorkspacesByContext<TContext>()
        where TContext : IWorkspaceContext
    {
        foreach (var workspace in _allWorkspaces)
        {
            var context = workspace.Context;
            if (context is not TContext actualContext) continue;

            yield return (workspace, actualContext);
        }
    }

    public bool TryGetWorkspaceByContext<TContext>([NotNullWhen(true)] out IWorkspaceViewModel? workspace)
        where TContext : IWorkspaceContext
    {
        return _allWorkspaces.TryGetFirst(workspace => workspace.Context is TContext, out workspace);
    }

    private bool TryGetPanel(IWorkspaceViewModel workspaceViewModel, PanelId panelId,
        [NotNullWhen(true)] out IPanelViewModel? panelViewModel)
    {
        panelViewModel = workspaceViewModel.Panels.FirstOrDefault(panel => panel.Id == panelId);
        if (panelViewModel is null)
        {
            _logger.LogError("Failed to find Panel with ID {PanelID} in Workspace with ID {WorkspaceID}", panelId,
                workspaceViewModel.Id);
            return false;
        }

        return true;
    }

    private bool TryGetTab(IPanelViewModel panelViewModel, PanelTabId tabId,
        [NotNullWhen(true)] out IPanelTabViewModel? tabViewModel)
    {
        tabViewModel = panelViewModel.Tabs.FirstOrDefault(tab => tab.Id == tabId);
        if (tabViewModel is null)
        {
            _logger.LogError(
                "Failed to find Tab with ID {TabID} in Panel with ID {PanelID} in Workspace with ID {WorkspaceID}",
                tabId, panelViewModel.Id, panelViewModel.WorkspaceId);
            return false;
        }

        return true;
    }

    public void ChangeActiveWorkspace(WorkspaceId workspaceId)
    {
        Dispatcher.UIThread.VerifyAccess();

        foreach (var workspace in AllWorkspaces)
        {
            if (workspace.Id == workspaceId)
            {
                workspace.IsActive = true;
                ActiveWorkspace = workspace;
            }
            else
            {
                workspace.IsActive = false;
            }
        }
    }

    /// <inheritdoc/>
    public IWorkspaceViewModel ChangeOrCreateWorkspaceByContext<TContext>(Func<Optional<PageData>> getPageData)
        where TContext : IWorkspaceContext, new()
    {
        Dispatcher.UIThread.VerifyAccess();

        if (!TryGetWorkspaceByContext<TContext>(out var existingWorkspace))
        {
            var pageData = getPageData();

            var newWorkspace = CreateWorkspace(
                new TContext(),
                pageData
            );

            existingWorkspace = newWorkspace;
        }

        ChangeActiveWorkspace(existingWorkspace.Id);
        return existingWorkspace;
    }

    /// <inheritdoc/>
    public IWorkspaceViewModel ChangeOrCreateWorkspaceByContext<TContext>(Func<TContext, bool> predicate,
        Func<Optional<PageData>> getPageData,
        Func<TContext> getWorkspaceContext) where TContext : IWorkspaceContext
    {
        Dispatcher.UIThread.VerifyAccess();

        var workspaces = FindWorkspacesByContext<TContext>();
        var existingWorkspace = workspaces.FirstOrOptional(tuple => predicate(tuple.Item2));

        if (!existingWorkspace.HasValue)
        {
            var newWorkspace = CreateWorkspace(
                getWorkspaceContext(),
                getPageData()
            );

            ChangeActiveWorkspace(newWorkspace.Id);
            return newWorkspace;
        }

        ChangeActiveWorkspace(existingWorkspace.Value.Item1.Id);
        return existingWorkspace.Value.Item1;
    }

    public void AddPanel(WorkspaceId workspaceId, WorkspaceGridState newWorkspaceState, AddPanelBehavior behavior)
    {
        Dispatcher.UIThread.VerifyAccess();

        if (!TryGetWorkspace(workspaceId, out WorkspaceViewModel? workspaceViewModel)) return;
        workspaceViewModel.AddPanel(newWorkspaceState, behavior);
    }

    public void OpenPage(WorkspaceId workspaceId, Optional<PageData> pageData, OpenPageBehavior behavior,
        bool selectTab = true)
    {
        Dispatcher.UIThread.VerifyAccess();

        if (!TryGetWorkspace(workspaceId, out WorkspaceViewModel? workspaceViewModel)) return;
        workspaceViewModel.OpenPage(pageData, behavior, selectTab);
    }

    public void SwapPanels(WorkspaceId workspaceId, PanelId firstPanelId, PanelId secondPanelId)
    {
        Dispatcher.UIThread.VerifyAccess();

        if (!TryGetWorkspace(workspaceId, out WorkspaceViewModel? workspaceViewModel)) return;
        workspaceViewModel.SwapPanels(firstPanelId, secondPanelId);
    }

    public void ClosePanel(WorkspaceId workspaceId, PanelId panelToClose)
    {
        Dispatcher.UIThread.VerifyAccess();

        if (!TryGetWorkspace(workspaceId, out WorkspaceViewModel? workspaceViewModel)) return;
        workspaceViewModel.ClosePanel(panelToClose);
    }

    public void SetTabTitle(string title, WorkspaceId workspaceId, PanelId panelId, PanelTabId tabId)
    {
        Dispatcher.UIThread.VerifyAccess();

        if (!TryGetWorkspace(workspaceId, out WorkspaceViewModel? workspaceViewModel)) return;
        if (!TryGetPanel(workspaceViewModel, panelId, out var panelViewModel)) return;
        if (!TryGetTab(panelViewModel, tabId, out var tabViewModel)) return;

        tabViewModel.Header.Title = title;
    }

    public void SetIcon(IconValue? icon, WorkspaceId workspaceId, PanelId panelId, PanelTabId tabId)
    {
        Dispatcher.UIThread.VerifyAccess();

        if (!TryGetWorkspace(workspaceId, out WorkspaceViewModel? workspaceViewModel)) return;
        if (!TryGetPanel(workspaceViewModel, panelId, out var panelViewModel)) return;
        if (!TryGetTab(panelViewModel, tabId, out var tabViewModel)) return;

        tabViewModel.Header.Icon = icon;
    }

    public OpenPageBehavior GetDefaultOpenPageBehavior()
    {
        // TODO: Query user settings for default behaviors for:
        // specific pages, specific interaction (mouse button modifiers), source workspace (if popped out or not)

        // Current default behavior is to replace the first tab in the first panel
        return new OpenPageBehavior.ReplaceTab(Optional<PanelId>.None, Optional<PanelTabId>.None);
    }
}
