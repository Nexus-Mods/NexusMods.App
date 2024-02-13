using DynamicData.Kernel;
using JetBrains.Annotations;
using OneOf;

namespace NexusMods.App.UI.WorkspaceSystem;

/// <summary>
/// Behavior for opening a new page in the workspace.
/// </summary>
[PublicAPI]
public class OpenPageBehavior : OneOfBase<
    OpenPageBehavior.ReplaceTab,
    OpenPageBehavior.NewTab,
    OpenPageBehavior.NewPanel,
    OpenPageBehavior.PrimaryDefault,
    OpenPageBehavior.SecondaryDefault>
{
    /// <summary>
    /// Replace the page in the given tab inside the given panel.
    /// </summary>
    /// <param name="PanelId">Optional ID of the panel, if this is <see cref="Optional{T}.None"/> the first panel will be used.</param>
    /// <param name="TabId">Optional ID of the tab, if this is <see cref="Optional{T}.None"/> the first tab of the panel will be used.</param>
    public record ReplaceTab(Optional<PanelId> PanelId, Optional<PanelTabId> TabId);

    /// <summary>
    /// Open the page in a new tab in the given panel.
    /// </summary>
    /// <param name="PanelId">Optional ID of the panel, if this is <see cref="Optional{T}.None"/> the first panel will be used.</param>
    public record NewTab(Optional<PanelId> PanelId);

    /// <summary>
    /// Opens the page in a new panel.
    /// </summary>
    /// <param name="NewWorkspaceState">Optional new workspace state, if this is <see cref="Optional{T}.None"/> the first possible state will be used.</param>
    public record NewPanel(Optional<WorkspaceGridState> NewWorkspaceState);

    /// <summary>
    /// Opens the page in using the primary default behavior (e.g. when normal click is used).
    /// </summary>
    public record PrimaryDefault;

    /// <summary>
    /// Opens the page in using the secondary default behavior (e.g. when middle click is used).
    /// </summary>
    public record SecondaryDefault;

    public OpenPageBehavior(OneOf<ReplaceTab, NewTab, NewPanel, PrimaryDefault, SecondaryDefault> input) : base(input) { }
}
