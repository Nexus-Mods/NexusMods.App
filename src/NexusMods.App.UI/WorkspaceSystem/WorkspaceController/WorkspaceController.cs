using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Threading;
using DynamicData;
using DynamicData.Kernel;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.App.UI.Controls.Navigation;
using NexusMods.Icons;
using NexusMods.App.UI.Extensions;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceAttachments;
using NexusMods.Extensions.BCL;
using NexusMods.Telemetry;
using ReactiveUI;

namespace NexusMods.App.UI.WorkspaceSystem;

[UsedImplicitly]
internal sealed class WorkspaceController : ReactiveObject, IWorkspaceController
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IWorkspaceWindow _window;
    private readonly ILogger _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IWorkspaceAttachmentsFactoryManager _workspaceAttachmentsFactory;
    private readonly PageFactoryController _pageFactoryController;

    public WindowId WindowId => _window.WindowId;

    private readonly SourceCache<WorkspaceViewModel, WorkspaceId> _workspaces = new(x => x.Id);
    private readonly ReadOnlyObservableCollection<IWorkspaceViewModel> _allWorkspaces;
    public ReadOnlyObservableCollection<IWorkspaceViewModel> AllWorkspaces => _allWorkspaces;

    private IWorkspaceViewModel? _activeWorkspace;
    public IWorkspaceViewModel ActiveWorkspace => GetActiveWorkspace();

    public WorkspaceController(IWorkspaceWindow window, IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _window = window;

        _loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        _logger = _loggerFactory.CreateLogger<WorkspaceController>();
        _workspaceAttachmentsFactory = serviceProvider.GetRequiredService<IWorkspaceAttachmentsFactoryManager>();
        _pageFactoryController = serviceProvider.GetRequiredService<PageFactoryController>();

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
            ActiveWorkspaceId = ActiveWorkspace.Id,
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
            var isActiveWorkspace = workspaceData.Id == data.ActiveWorkspaceId;

            var context = workspaceData.Context;
            if (!context.IsValid(_serviceProvider))
            {
                _logger.LogWarning("Workspace with Context {Context} ({Type}) is no longer valid and has been removed", context, context.GetType());
                continue;
            }

            var vm = CreateWorkspace(
                context: Optional<IWorkspaceContext>.Create(workspaceData.Context),
                pageData: Optional<PageData>.None
            );

            try
            {
                vm.FromData(workspaceData);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Exception while restoring Workspace `{WorkspaceId}`, resetting to default", workspaceData.Id);

                vm.FromData(new WorkspaceData
                {
                    Context = vm.Context,
                    Id = vm.Id,
                    Panels = [
                        new PanelData
                        {
                            LogicalBounds = MathUtils.One,
                            Tabs = [],
                            SelectedTabId = PanelTabId.DefaultValue,
                        },
                    ],
                });
            }

            if (isActiveWorkspace) activeWorkspace = vm;
        }

        if (_workspaces.Count == 0)
        {
            _logger.LogInformation("Restored zero workspaces, creating default workspace");
            CreateWorkspace(context: new HomeContext(), pageData: Optional<PageData>.None);
        }

        ChangeActiveWorkspace(activeWorkspace?.Id ?? _workspaces.Keys.First());
    }

    public IWorkspaceViewModel CreateWorkspace(Optional<IWorkspaceContext> context, Optional<PageData> pageData)
    {
        Dispatcher.UIThread.VerifyAccess();

        var vm = new WorkspaceViewModel(
            loggerFactory: _loggerFactory,
            workspaceController: this,
            factoryController: _pageFactoryController
        )
        {
            Context = context.HasValue ? context.Value : EmptyContext.Instance,
        };

        vm.Title = _workspaceAttachmentsFactory.CreateTitleFor(vm.Context);
        vm.Subtitle = _workspaceAttachmentsFactory.CreateSubtitleFor(vm.Context);

        _workspaces.AddOrUpdate(vm);

        var addPanelBehavior = pageData.HasValue
            ? new AddPanelBehavior(new AddPanelBehavior.WithCustomTab(pageData.Value))
            : new AddPanelBehavior(new AddPanelBehavior.WithDefaultTab());

        vm.AddPanel(
            WorkspaceGridState.From(new[]
            {
                new PanelGridState(PanelId.DefaultValue, MathUtils.One),
            }, isHorizontal: vm.IsHorizontal),
            addPanelBehavior
        );

        return vm;
    }

    public void UnregisterWorkspaceByContext<TContext>(Func<TContext, bool> predicate)
        where TContext : IWorkspaceContext
    {
        Dispatcher.UIThread.VerifyAccess();

        var workspaces = FindWorkspacesByContext<TContext>();
        var existingWorkspace = workspaces.FirstOrOptional(tuple => predicate(tuple.Item2));
        if (!existingWorkspace.HasValue) return;
        var workspace = existingWorkspace.Value.Item1;

        UnregisterWorkspace(workspace);
    }

    private void UnregisterWorkspace(IWorkspaceViewModel workspaceViewModel)
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
                _activeWorkspace = workspace;
            }
            else
            {
                workspace.IsActive = false;
            }
        }

        this.RaisePropertyChanged(nameof(ActiveWorkspace));
    }

    private IWorkspaceViewModel GetActiveWorkspace()
    {
        if (_activeWorkspace is null) throw new InvalidOperationException("There is no active workspace");
        return _activeWorkspace;
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

    public void OpenPage(
        WorkspaceId workspaceId,
        Optional<PageData> pageData,
        OpenPageBehavior behavior,
        bool selectTab = true,
        bool checkOtherPanels = true)
    {
        Dispatcher.UIThread.VerifyAccess();

        if (!TryGetWorkspace(workspaceId, out WorkspaceViewModel? workspaceViewModel)) return;
        workspaceViewModel.OpenPage(pageData, behavior, selectTab, checkOtherPanels);

        if (Tracking.IsEnabled && pageData.HasValue)
        {
            var pageType = pageData.Value.Context.TrackingName;
            var eventDefinition = behavior.Match(
                f0: _ => Events.Page.ReplaceTab,
                f1: _ => Events.Page.NewTab,
                f2: _ => Events.Page.NewPanel
            );

            Tracking.AddEvent(eventDefinition, new EventMetadata(name: pageType));
        }
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

        tabViewModel.Header.Icon = icon ?? new IconValue();
    }

    /// <inheritdoc/>
    public OpenPageBehavior GetOpenPageBehavior(PageData requestedPage, NavigationInformation navigationInformation)
    {
        if (navigationInformation.OpenPageBehaviorType.TryGet(out var behaviorType))
        {
            return CreateOpenPageBehavior(behaviorType);
        }

        return GetDefaultOpenPageBehavior(requestedPage, navigationInformation.Input);
    }

    /// <inheritdoc/>
    public OpenPageBehavior GetDefaultOpenPageBehavior(PageData requestedPage, NavigationInput input)
    {
        const OpenPageBehaviorType fallback = OpenPageBehaviorType.ReplaceTab;

        var requestedPageFactory = _pageFactoryController.GetFactory(requestedPage);

        var isPrimaryInput = input.IsPrimaryInput();
        var globalSettings = OpenPageBehaviorSettings.Default;
        var pageDefaultBehavior = requestedPageFactory.DefaultOpenPageBehavior;

        var globalBehaviorType = globalSettings.GetValueOrDefault(input, fallback);

        var behaviorType = isPrimaryInput
            ? pageDefaultBehavior.ValueOr(globalBehaviorType)
            : globalBehaviorType;

        return CreateOpenPageBehavior(behaviorType);
    }

    private OpenPageBehavior CreateOpenPageBehavior(OpenPageBehaviorType behaviorType)
    {
        var selectedPanel = ActiveWorkspace.SelectedPanel;
        var selectedTab = selectedPanel.SelectedTab;

        return behaviorType switch
        {
            OpenPageBehaviorType.ReplaceTab => new OpenPageBehavior.ReplaceTab(selectedPanel.Id, selectedTab.Id),
            OpenPageBehaviorType.NewTab => new OpenPageBehavior.NewTab(selectedPanel.Id),
            OpenPageBehaviorType.NewPanel => new OpenPageBehavior.NewPanel(Optional<WorkspaceGridState>.None),
        };
    }

    public PageData GetDefaultPageData(WorkspaceId workspaceId)
    {
        if (!TryGetWorkspace(workspaceId, out WorkspaceViewModel? workspace)) throw new InvalidOperationException();
        return workspace.GetDefaultPageData();
    }
}
