using System.Diagnostics;
using System.Reactive;
using DynamicData.Kernel;
using NexusMods.Abstractions.UI;
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
        var pageData = Contents.PageData;
        if (pageData.Context.IsEphemeral)
        {
            var serializablePageData = pageData.Context.GetSerializablePageData();
            if (serializablePageData is null) return null;

            Debug.Assert(!serializablePageData.Context.IsEphemeral);
            pageData = serializablePageData;
        }

        return new TabData
        {
            Id = Id,
            PageData = pageData,
        };
    }
}
