using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
// ReSharper disable All

namespace Examples.Workspaces;

// This class shows how to query Windows and Workspace from outside a page.
// See the end of the file for an example on how to query these types from
// within a page.
file class ExampleOutsideAPage
{
    private readonly IWindowManager _windowManager;

    public ExampleOutsideAPage(IWindowManager windowManager)
    {
        // The IWindowManager implementation is registered with DI as a singleton.
        _windowManager = windowManager;
    }

    public void Do()
    {
        // Most of the time, you'll want to get the currently active Window.
        // The active Window is the one that the user is currently interacting
        // with.
        if (!_windowManager.TryGetActiveWindow(out var activeWindow)) return;

        // If you need to get a specific Window, you can refer to it by it's
        // WindowId.
        if (!_windowManager.TryGetWindow(WindowId.DefaultValue, out var myWindow)) return;

        var workspaceController = activeWindow.WorkspaceController;

        // With the Workspace Controller, you can easily query Workspaces:
        // 1) By a unique Context. This assumes that there's only one Workspace
        //    that has this Context.
        workspaceController.TryGetWorkspaceByContext<ExampleContext>(out var workspace);

        // 2) By a non-unique Context. This will return all Workspaces that have
        //    this Context.
        (IWorkspaceViewModel, ExampleContext)[] workspaces = workspaceController
            .FindWorkspacesByContext<ExampleContext>()
            .ToArray();

        // 3) By Id.
        if (!workspaceController.TryGetWorkspace(WorkspaceId.DefaultValue, out var myWorkspace)) return;

        // 4) By active Workspace. Note that this property is reactive, so you
        //    can be notified of changes.
        var activeWorkspace = workspaceController.ActiveWorkspace;
    }
}

file record ExampleContext : IWorkspaceContext;

// This example class shows how to query Windows and Workspaces from within a page.
file class ExamplePage : APageViewModel<IMyPageInterface>, IMyPageInterface
{
    public ExamplePage(IWindowManager windowManager) : base(windowManager) { }

    public void Do()
    {
        // A Page knows it's own location. This makes it easy to query the exact
        // objects that you need using these IDs:
        var windowId = base.WindowId;
        var workspaceId = base.WorkspaceId;
        var panelId = base.PanelId;
        var tabId = base.TabId;

        // APageViewModel<TVM> also has a utility method to quickly get the
        // current Workspace Controller.
        var workspaceController = base.GetWorkspaceController();
    }
}

file interface IMyPageInterface : IPageViewModelInterface;
