using DynamicData.Binding;
using JetBrains.Annotations;

namespace NexusMods.Abstractions.Settings;

/// <summary>
/// Helper abstract class for <see cref="SettingsPropertyValueContainer"/>.
/// </summary>
[PublicAPI]
public abstract class APropertyValueContainer<T> : AbstractNotifyPropertyChanged
{
    private T _previousValue;
    private T _currentValue;
    private bool _hasChanged;

    private IEqualityComparer<T> _equalityComparer;

    /// <summary>
    /// Constructor.
    /// </summary>
    protected APropertyValueContainer(T value, IEqualityComparer<T>? equalityComparer = null)
    {
        _previousValue = value;
        _currentValue = value;
        _hasChanged = false;
        _equalityComparer = equalityComparer ?? EqualityComparer<T>.Default;
    }

    /// <summary>
    /// Gets the previous value.
    /// </summary>
    public T PreviousValue => _previousValue;

    /// <summary>
    /// Gets or sets the current value.
    /// </summary>
    public T CurrentValue
    {
        get => _currentValue;
        set => SetAndRaise(ref _currentValue, value, _equalityComparer);
    }

    /// <summary>
    /// Gets whether <see cref="CurrentValue"/> is different from <see cref="PreviousValue"/>.
    /// </summary>
    public bool HasChanged
    {
        get => _hasChanged;
        private set => SetAndRaise(ref _hasChanged, value);
    }

    /// <inheritdoc/>
    protected override void OnPropertyChanged(string? propertyName = null)
    {
        if (propertyName == nameof(CurrentValue))
        {
            HasChanged = _equalityComparer.Equals(PreviousValue, CurrentValue);
        }

        base.OnPropertyChanged(propertyName);
    }
}
