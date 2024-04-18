using JetBrains.Annotations;

namespace NexusMods.Abstractions.Settings;

/// <summary>
/// Container for a singe value with multiple choices.
/// </summary>
[PublicAPI]
public sealed class SingleValueMultipleChoiceContainer : APropertyValueContainer<object>
{
    private readonly Func<object, object> _valueToKey;
    private readonly Func<object, object> _keyToValue;
    private readonly object[] _allowedValues;
    private readonly Func<object, string> _valueToTranslation;

    /// <summary>
    /// Constructor.
    /// </summary>
    public SingleValueMultipleChoiceContainer(
        object key,
        Func<object, object> valueToKey,
        Func<object, object> keyToValue,
        IEqualityComparer<object> keyComparer,
        object[] allowedValues,
        Func<object, string> valueToTranslation) : base(key, keyComparer)
    {
        _valueToKey = valueToKey;
        _keyToValue = keyToValue;
        _allowedValues = allowedValues;
        _valueToTranslation = valueToTranslation;
    }
}
