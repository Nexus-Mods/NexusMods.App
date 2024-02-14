using TransparentValueObjects;

namespace NexusMods.App.UI.WorkspaceSystem;

[ValueObject<Guid>]
public readonly partial struct PanelTabId : IAugmentWith<DefaultValueAugment, JsonAugment>
{
    /// <inheritdoc/>
    public static PanelTabId DefaultValue { get; } = From(Guid.Empty);
}
