using System.Reactive;
using DynamicData.Kernel;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.WorkspaceSystem;

public class PanelTabViewModel : AViewModel<IPanelTabViewModel>, IPanelTabViewModel
{
    /// <inheritdoc/>
    public PanelTabId Id { get; }

    /// <inheritdoc/>
    public IPanelTabHeaderViewModel Header { get; }

    /// <inheritdoc/>
    [Reactive] public required Page Contents { get; set; }

    /// <inheritdoc/>
    [Reactive] public bool IsVisible { get; set; } = true;

    /// <inheritdoc/>
    public ReactiveCommand<Unit, Unit> GoBackInHistoryCommand => History.GoBackCommand;

    /// <inheritdoc/>
    public ReactiveCommand<Unit, Unit> GoForwardInHistoryCommand => History.GoForwardCommand;

    private TabHistory History { get; }

    public PanelTabViewModel(IWorkspaceController workspaceController, WorkspaceId workspaceId, PanelId panelId, Optional<PanelTabId> tabId = default)
    {
        Id = tabId.HasValue ? tabId.Value : PanelTabId.From(Guid.NewGuid());
        Header = new PanelTabHeaderViewModel(Id);
        History = new TabHistory(
            openPageFunc: pageData => {
                workspaceController.OpenPage(workspaceId, pageData, behavior: new OpenPageBehavior(new OpenPageBehavior.ReplaceTab(panelId, Header.Id)), selectTab: true, checkOtherPanels: false);
            }
        );

        this.WhenAnyValue(vm => vm.Contents)
            .WhereNotNull()
            .SubscribeWithErrorLogging(page => History.AddToHistory(page.PageData));
    }

    public TabData? ToData()
    {
        if (Contents.PageData.Context.IsEphemeral) return null;

        return new TabData
        {
            Id = Id,
            PageData = Contents.PageData,
        };
    }
}
