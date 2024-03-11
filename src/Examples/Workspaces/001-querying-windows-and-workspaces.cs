using System.Diagnostics.CodeAnalysis;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;

namespace Examples.Workspaces;

[SuppressMessage("ReSharper", "UnusedType.Local", Justification = "Example")]
[SuppressMessage("ReSharper", "UnusedVariable", Justification = "Example")]
[SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Example")]
[SuppressMessage("ReSharper", "SuggestVarOrType_Elsewhere", Justification = "Example")]
file class Example
{
    private readonly IWindowManager _windowManager;

    public Example(IWindowManager windowManager)
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

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Local", Justification = "Example")]
file record ExampleContext : IWorkspaceContext;
