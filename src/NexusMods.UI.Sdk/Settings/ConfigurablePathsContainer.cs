using NexusMods.Sdk;
using NexusMods.Sdk.Settings;

namespace NexusMods.UI.Sdk.Settings;

public class ConfigurablePathsContainer : APropertyValueContainer<ConfigurablePath[], ConfigurablePathsContainerOption>
{
    public ConfigurablePathsContainer(
        ConfigurablePath[] value,
        ConfigurablePath[] defaultValue,
        PropertyConfig config) : base(value, defaultValue, config)
    {
    }
}
