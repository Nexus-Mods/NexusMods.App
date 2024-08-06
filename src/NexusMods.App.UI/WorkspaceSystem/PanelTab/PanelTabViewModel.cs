using System.Reactive;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.WorkspaceSystem;

public class PanelTabViewModel : AViewModel<IPanelTabViewModel>, IPanelTabViewModel
{
    /// <inheritdoc/>
    public PanelTabId Id { get; } = PanelTabId.From(Guid.NewGuid());

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

    public PanelTabViewModel(IWorkspaceController workspaceController, WorkspaceId workspaceId, PanelId panelId)
    {
        Header = new PanelTabHeaderViewModel(Id);
        History = new TabHistory(
            openPageFunc: pageData => {
                workspaceController.OpenPage(workspaceId, pageData, behavior: new OpenPageBehavior(new OpenPageBehavior.ReplaceTab(panelId, Header.Id)), selectTab: true, checkOtherPanels: false);
            }
        );

        this.WhenAnyValue(vm => vm.Contents)
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
