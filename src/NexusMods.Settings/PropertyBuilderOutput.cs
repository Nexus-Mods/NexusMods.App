using System.Diagnostics;
using NexusMods.Abstractions.Settings;

namespace NexusMods.Settings;

internal interface IPropertyBuilderOutput
{
    SectionId SectionId { get; }
    string DisplayName { get; }
    string Description { get; }
    Uri? Link { get; }
    bool RequiresRestart { get; }
    string? RestartMessage { get; }

    ISettingsPropertyValueContainerFactory Factory { get; }

    object GetValue(SettingsManager settingsManager);
    object GetDefaultValue(SettingsManager settingsManager);
    void Update(ISettingsManager settingsManager, object newValue);
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

    TProperty CoreGetValue(SettingsManager settingsManager);
    TProperty CoreGetDefaultValue(SettingsManager settingsManager);
    void CoreUpdate(ISettingsManager settingsManager, TProperty value);
}

internal record PropertyBuilderOutput<TSettings, TProperty>(
    SectionId SectionId,
    string DisplayName,
    string Description,
    Uri? Link,
    bool RequiresRestart,
    string? RestartMessage,
    ISettingsPropertyValueContainerFactory Factory,
    Func<TSettings, TProperty> SelectorFunc,
    Delegate PropertySetterDelegate) : IPropertyBuilderOutput<TProperty>
    where TSettings : class, ISettings, new()
    where TProperty : notnull
{
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
}
