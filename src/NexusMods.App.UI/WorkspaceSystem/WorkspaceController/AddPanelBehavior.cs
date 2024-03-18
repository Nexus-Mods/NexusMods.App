using JetBrains.Annotations;
using OneOf;

namespace NexusMods.App.UI.WorkspaceSystem;

/// <summary>
/// Behavior for adding a new panel.
/// </summary>
[PublicAPI]
public class AddPanelBehavior : OneOfBase<
    AddPanelBehavior.WithDefaultTab,
    AddPanelBehavior.WithCustomTab>
{
    /// <summary>
    /// Add a new panel with the default tab.
    /// </summary>
    public record WithDefaultTab;

    /// <summary>
    /// Add a new panel with a custom tab.
    /// </summary>
    /// <param name="PageData"></param>
    public record WithCustomTab(PageData PageData);

    public AddPanelBehavior(OneOf<WithDefaultTab, WithCustomTab> input) : base(input) { }
}
