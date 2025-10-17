using JetBrains.Annotations;
using NexusMods.Sdk.Settings;

namespace NexusMods.UI.Sdk.Settings;

[PublicAPI]
public class BooleanContainer : APropertyValueContainer<bool, BooleanContainerOptions>
{
    public BooleanContainer(
        bool value,
        bool defaultValue,
        PropertyConfig config) : base(value, defaultValue, config) { }
}
