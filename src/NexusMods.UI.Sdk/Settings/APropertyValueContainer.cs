using DynamicData.Binding;
using JetBrains.Annotations;
using NexusMods.Sdk.Settings;

namespace NexusMods.UI.Sdk.Settings;

/// <summary>
/// Base implementation of <see cref="IPropertyValueContainer"/>.
/// </summary>
[PublicAPI]
public abstract class APropertyValueContainer<T, TOptions> : AbstractNotifyPropertyChanged, IPropertyValueContainer
    where T : notnull
    where TOptions : IContainerOptions
{
    private T _previousValue;
    private T _currentValue;
    private bool _hasChanged;
    private bool _isDefault;
    private ValidationResult _validationResult;
    private readonly PropertyConfig _propertyConfig;

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
        PropertyConfig config,
        IEqualityComparer<T>? equalityComparer = null)
    {
        DefaultValue = defaultValue;

        _previousValue = value;
        _currentValue = value;

        _propertyConfig = config;

        EqualityComparer = equalityComparer ?? EqualityComparer<T>.Default;

        _hasChanged = false;
        _isDefault = EqualityComparer.Equals(value, defaultValue);

        _validationResult = Validate(value);
    }

    private ValidationResult Validate(T value)
    {
        var validator = _propertyConfig.Options.Validation ?? DefaultValidator;
        return validator(value);
    }

    private static ValidationResult DefaultValidator(object value) => ValidationResult.CreateSuccessful();

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
    object IPropertyValueContainer.CurrentValue => CurrentValue;

    /// <summary>
    /// Gets whether <see cref="CurrentValue"/> is different from <see cref="PreviousValue"/>.
    /// </summary>
    public bool HasChanged
    {
        get => _hasChanged;
        private set => SetAndRaise(ref _hasChanged, value);
    }

    /// <inheritdoc/>
    public ValidationResult ValidationResult
    {
        get => _validationResult;
        private set => SetAndRaise(ref _validationResult, value);
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
        if (propertyName is nameof(CurrentValue))
        {
            ValidationResult = Validate(CurrentValue);
        }

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
        _propertyConfig.Update(settingsManager, CurrentValue);
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

