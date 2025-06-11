using DynamicData.Binding;
using JetBrains.Annotations;

namespace NexusMods.Abstractions.Settings;

/// <summary>
/// Helper abstract class for <see cref="SettingsPropertyValueContainer"/>.
/// </summary>
[PublicAPI]
public abstract class APropertyValueContainer<T> : AbstractNotifyPropertyChanged, IValueContainer
    where T : notnull
{
    private T _previousValue;
    private T _currentValue;
    private bool _hasChanged;
    private bool _isDefault;
    private readonly Action<ISettingsManager, T> _updaterFunc;

    /// <summary>
    /// Gets the equality comparer.
    /// </summary>
    public IEqualityComparer<T> EqualityComparer { get; }

    /// <summary>
    /// Constructor.
    /// </summary>
    protected APropertyValueContainer(
        T value,
        T defaultValue,
        Action<ISettingsManager, T> updaterFunc,
        IEqualityComparer<T>? equalityComparer = null)
    {
        DefaultValue = defaultValue;

        _previousValue = value;
        _currentValue = value;

        _updaterFunc = updaterFunc;
        EqualityComparer = equalityComparer ?? EqualityComparer<T>.Default;

        _hasChanged = false;
        _isDefault = EqualityComparer.Equals(value, defaultValue);
    }

    /// <summary>
    /// Gets the default value.
    /// </summary>
    public T DefaultValue { get; }

    /// <summary>
    /// Gets the previous value.
    /// </summary>
    public T PreviousValue
    {
        get => _previousValue;
        set => SetAndRaise(ref _previousValue, value, EqualityComparer);
    }

    /// <summary>
    /// Gets or sets the current value.
    /// </summary>
    public T CurrentValue
    {
        get => _currentValue;
        set => SetAndRaise(ref _currentValue, value, EqualityComparer);
    }

    /// <inheritdoc/>
    object IValueContainer.CurrentValue => CurrentValue;

    /// <summary>
    /// Gets whether <see cref="CurrentValue"/> is different from <see cref="PreviousValue"/>.
    /// </summary>
    public bool HasChanged
    {
        get => _hasChanged;
        private set => SetAndRaise(ref _hasChanged, value);
    }

    /// <summary>
    /// Gets whether <see cref="CurrentValue"/> equals <see cref="DefaultValue"/>.
    /// </summary>
    public bool IsDefault
    {
        get => _isDefault;
        private set => SetAndRaise(ref _isDefault, value);
    }

    /// <inheritdoc/>
    protected override void OnPropertyChanged(string? propertyName = null)
    {
        if (propertyName is nameof(CurrentValue) or nameof(PreviousValue))
        {
            HasChanged = !EqualityComparer.Equals(PreviousValue, CurrentValue);
        }

        if (propertyName is nameof(CurrentValue) or nameof(DefaultValue))
        {
            IsDefault = EqualityComparer.Equals(CurrentValue, DefaultValue);
        }

        base.OnPropertyChanged(propertyName);
    }

    /// <inheritdoc/>
    public void Update(ISettingsManager settingsManager)
    {
        _updaterFunc(settingsManager, CurrentValue);
        PreviousValue = CurrentValue;
    }

    /// <inheritdoc/>
    public void ResetToPrevious()
    {
        CurrentValue = PreviousValue;
    }

    /// <inheritdoc/>
    public void ResetToDefault()
    {
        CurrentValue = DefaultValue;
    }
}
