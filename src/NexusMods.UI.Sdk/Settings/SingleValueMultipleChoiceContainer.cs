using JetBrains.Annotations;
using NexusMods.Sdk.Settings;

namespace NexusMods.UI.Sdk.Settings;

[PublicAPI]
public class SingleValueMultipleChoiceContainer : APropertyValueContainer<object, SingleValueMultipleChoiceContainerOptions>
{
    /// <summary>
    /// Constructor.
    /// </summary>
    public SingleValueMultipleChoiceContainer(
        object value,
        object defaultValue,
        PropertyConfig config,
        SingleValueMultipleChoiceContainerOptions options) : base(value, defaultValue, config, equalityComparer: options.ValueComparer)
    {
        Values = options.AllowedValues.Select(x => new KeyValuePair<object, string>(x, options.ValueToDisplayString(x))).ToArray();
    }

    public KeyValuePair<object, string>[] Values { get; }
}
