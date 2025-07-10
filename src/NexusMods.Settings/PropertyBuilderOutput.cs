using System.Diagnostics;
using NexusMods.Abstractions.Settings;

namespace NexusMods.Settings;

internal interface IPropertyBuilderOutput
{
    SectionId SectionId { get; }
    string DisplayName { get; }
    Func<object, string> DescriptionFactory { get; }
    Uri? Link { get; }
    bool RequiresRestart { get; }
    string? RestartMessage { get; }

    ISettingsPropertyValueContainerFactory Factory { get; }

    object GetValue(SettingsManager settingsManager);
    object GetDefaultValue(SettingsManager settingsManager);
    void Update(ISettingsManager settingsManager, object newValue);
    ValidationResult Validate(object value);
}

internal interface IPropertyBuilderOutput<TProperty> : IPropertyBuilderOutput where TProperty : notnull
{
    object IPropertyBuilderOutput.GetValue(SettingsManager settingsManager) => CoreGetValue(settingsManager);
    object IPropertyBuilderOutput.GetDefaultValue(SettingsManager settingsManager) => CoreGetDefaultValue(settingsManager);
    void IPropertyBuilderOutput.Update(ISettingsManager settingsManager, object newValue)
    {
        Debug.Assert(newValue.GetType() == typeof(TProperty));
        CoreUpdate(settingsManager, (TProperty)newValue);
    }

    ValidationResult IPropertyBuilderOutput.Validate(object value)
    {
        Debug.Assert(value.GetType() == typeof(TProperty));
        return CoreValidate((TProperty)value);
    }

    TProperty CoreGetValue(SettingsManager settingsManager);
    TProperty CoreGetDefaultValue(SettingsManager settingsManager);
    void CoreUpdate(ISettingsManager settingsManager, TProperty value);
    ValidationResult CoreValidate(TProperty value);
}

internal record PropertyBuilderOutput<TSettings, TProperty>(
    SectionId SectionId,
    string DisplayName,
    Func<TProperty, string> GenericDescriptionFactory,
    Uri? Link,
    bool RequiresRestart,
    string? RestartMessage,
    ISettingsPropertyValueContainerFactory Factory,
    Func<TProperty, ValidationResult>? Validator,
    Func<TSettings, TProperty> SelectorFunc,
    Delegate PropertySetterDelegate) : IPropertyBuilderOutput<TProperty>
    where TSettings : class, ISettings, new()
    where TProperty : notnull
{
    public Func<object, string> DescriptionFactory => obj =>
    {
        if (obj is not TProperty property) throw new NotSupportedException();
        return GenericDescriptionFactory(property);
    };

    public TProperty CoreGetValue(SettingsManager settingsManager)
    {
        var settings = settingsManager.Get<TSettings>();
        return SelectorFunc.Invoke(settings);
    }

    public TProperty CoreGetDefaultValue(SettingsManager settingsManager)
    {
        var settings = settingsManager.GetDefaultValue<TSettings>();
        return SelectorFunc.Invoke(settings);
    }

    public void CoreUpdate(ISettingsManager settingsManager, TProperty newValue)
    {
        settingsManager.Update<TSettings>(settings =>
        {
            // void Set_Property(TSettings this, TProperty newValue)
            PropertySetterDelegate.DynamicInvoke([settings, newValue]);
            return settings;
        });
    }

    public ValidationResult CoreValidate(TProperty value)
    {
        if (Validator is null) return ValidationResult.CreateSuccessful();
        return Validator.Invoke(value);
    }
}
