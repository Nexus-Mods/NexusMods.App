using TransparentValueObjects;

namespace NexusMods.App.UI.WorkspaceSystem;

[ValueObject<Guid>]
public readonly partial struct WorkspaceId : IAugmentWith<DefaultValueAugment>
{
    /// <inheritdoc/>
    public static WorkspaceId DefaultValue { get; } = From(Guid.Empty);
}
