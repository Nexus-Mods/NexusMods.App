using JetBrains.Annotations;

namespace NexusMods.Abstractions.Settings;

/// <summary>
/// Container for a singe value with multiple choices.
/// </summary>
[PublicAPI]
public sealed class SingleValueMultipleChoiceContainer : APropertyValueContainer<object>
{
    private readonly object[] _allowedValues;
    private readonly Func<object, string> _valueToTranslation;

    /// <summary>
    /// Constructor.
    /// </summary>
    public SingleValueMultipleChoiceContainer(
        object value,
        object defaultValue,
        Action<ISettingsManager, object> updaterFunc,
        IEqualityComparer<object> valueComparer,
        object[] allowedValues,
        Func<object, string> valueToTranslation) : base(value, defaultValue, updaterFunc, equalityComparer: valueComparer)
    {
        _allowedValues = allowedValues;
        _valueToTranslation = valueToTranslation;

        Values = _allowedValues.Select(x => new KeyValuePair<object, string>(x, _valueToTranslation(x))).ToArray();
    }

    public KeyValuePair<object, string>[] Values { get; }
}
