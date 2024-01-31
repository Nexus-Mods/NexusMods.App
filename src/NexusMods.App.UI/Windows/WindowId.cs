using TransparentValueObjects;

namespace NexusMods.App.UI.Windows;

[ValueObject<Guid>]
public readonly partial struct WindowId : IAugmentWith<DefaultValueAugment, JsonAugment>
{
    /// <inheritdoc/>
    public static WindowId DefaultValue { get; } = From(Guid.Empty);
}
