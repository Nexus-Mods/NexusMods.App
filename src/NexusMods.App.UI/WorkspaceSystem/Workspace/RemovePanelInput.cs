namespace NexusMods.App.UI.WorkspaceSystem;

/// <summary>
/// Input for <see cref="IWorkspaceViewModel.RemovePanelCommand"/>.
/// </summary>
/// <param name="PanelToConsume">The panel that is going to be removed from the workspace.</param>
/// <param name="PanelToExpand">The panel that is going to take up the new space.</param>
public record struct RemovePanelInput(IPanelViewModel PanelToConsume, IPanelViewModel PanelToExpand);
