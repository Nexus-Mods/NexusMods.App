using TransparentValueObjects;

namespace NexusMods.App.UI.WorkspaceSystem;

[ValueObject<Guid>]
public readonly partial struct PanelId : IAugmentWith<DefaultValueAugment>
{
    /// <inheritdoc/>
    public static PanelId DefaultValue { get; } = From(Guid.Empty);
}
