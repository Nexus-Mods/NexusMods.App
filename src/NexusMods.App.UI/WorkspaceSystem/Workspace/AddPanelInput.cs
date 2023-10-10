namespace NexusMods.App.UI.WorkspaceSystem;

/// <summary>
/// Input for <see cref="IWorkspaceViewModel.AddPanelCommand"/>.
/// </summary>
/// <param name="PanelToSplit">The panel to split. Null is only allowed if the workspace contains no panels.</param>
/// <param name="SplitVertically">Whether to split the panel vertically or horizontally.</param>
public record struct AddPanelInput(IPanelViewModel? PanelToSplit, bool SplitVertically);
