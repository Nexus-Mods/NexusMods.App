using DynamicData.Kernel;
using NexusMods.App.UI.Controls;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
// ReSharper disable All

namespace Examples.Workspaces;

file class Example
{
    private readonly IWindowManager _windowManager;

    public Example(IWindowManager windowManager)
    {
        _windowManager = windowManager;
    }

    public void Do()
    {
        if (!_windowManager.TryGetActiveWindow(out var activeWindow)) return;
        var workspaceController = activeWindow.WorkspaceController;

        // The Workspace Controller requires a WorkspaceId for most of it's
        // methods. See the previous example on how to query the exact Workspace
        // that you need.
        var workspaceId = workspaceController.ActiveWorkspace!.Id;

        // The OpenPage method has two main arguments:

        // 1) The PageData argument:
        // If pageData is set to None, the Workspace Controller will instead
        // show the "default page".
        Optional<PageData> pageData = Optional<PageData>.None;

        // Most of the time, you want to open a specific page. In this case,
        // you have to provide the Workspace Controller with some PageData.
        // The next example will show you how to create new factories and pages.
        pageData = new PageData
        {
            FactoryId = DummyPageFactory.StaticId,
            Context = new DummyPageContext(),
        };

        // 2) The OpenPageBehavior:
        // The type OpenPageBehavior is a union between various types, and it's
        // value can only be one of those types. There are currently 3 different
        // behaviors. While you can specify exactly how a page should be opened,
        // it's recommended to use the default behavior:

        // GetDefaultOpenPageBehavior will figure out the best behavior for the
        // requested page depending on a variety of factors.
        OpenPageBehavior behavior = workspaceController.GetDefaultOpenPageBehavior(
            // The requested page itself can specify a default behavior.
            requestedPage: pageData.Value,
            // Depending on the input, different behaviors are desired.
            // TODO: https://github.com/Nexus-Mods/NexusMods.App/issues/942
            input: NavigationInput.Default,
            // Most of the time, the request to open a new page comes from
            // an existing Page. An exception would be something like the
            // Left Menu, which is outside the Workspace system.
            currentPage: Optional<PageIdBundle>.None
        );

        // 1) ReplaceTab will replace the page inside an existing tab.
        behavior = new OpenPageBehavior.ReplaceTab(
            // If this is None, the first panel of the Workspace will be selected.
            PanelId: Optional<PanelId>.None,
            // If this is None, the first tab of the Panel will be replaced.
            TabId: Optional<PanelTabId>.None
        );

        // 2) NewTab will create a new tab inside a Panel.
        behavior = new OpenPageBehavior.NewTab(
            // If this is None, the first panel of the Workspace will be selected.
            PanelId: Optional<PanelId>.None
        );

        // 3) NewPanel will create a new Panel inside the Workspace and add
        //    a new tab to the Panel.
        behavior = new OpenPageBehavior.NewPanel(
            // If this is None, the first possible state will be used.
            NewWorkspaceState: Optional<WorkspaceGridState>.None
        );

        workspaceController.OpenPage(
            workspaceId,
            pageData,
            behavior,
            // selectTab is set to true by default, so it can be omitted.
            selectTab: true
        );
    }
}
