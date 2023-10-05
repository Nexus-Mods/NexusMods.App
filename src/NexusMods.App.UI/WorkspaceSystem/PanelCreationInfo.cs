using Avalonia;

namespace NexusMods.App.UI.WorkspaceSystem;

public record PanelCreationInfo(IPanelViewModel PanelToSplit, Rect UpdatedLogicalBounds, Rect NewPanelLogicalBounds);
