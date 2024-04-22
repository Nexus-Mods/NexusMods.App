using NexusMods.Abstractions.Settings;

namespace NexusMods.Settings;

internal interface IPropertyBuilderOutput
{
    SectionId SectionId { get; }
    string DisplayName { get; }
    string Description { get; }
    bool RequiresRestart { get; }
    string? RestartMessage { get; }

    ISettingsPropertyValueContainerFactory Factory { get; }

    object GetValue(SettingsManager settingsManager);
    object GetDefaultValue(SettingsManager settingsManager);

    // TODO: Delegate PropertySetterDelegate,
    // TODO: Action<Delegate, ISettingsManager, object> UpdateAction,
}

internal interface IPropertyBuilderOutput<out TProperty> : IPropertyBuilderOutput
{
    object IPropertyBuilderOutput.GetValue(SettingsManager manager) => CoreGetValue(manager)!;
    object IPropertyBuilderOutput.GetDefaultValue(SettingsManager manager) => CoreGetDefaultValue(manager)!;

    TProperty CoreGetValue(SettingsManager settingsManager);
    TProperty CoreGetDefaultValue(SettingsManager settingsManager);
}

internal record PropertyBuilderOutput<TSettings, TProperty>(
    SectionId SectionId,
    string DisplayName,
    string Description,
    bool RequiresRestart,
    string? RestartMessage,
    ISettingsPropertyValueContainerFactory Factory,
    Func<TSettings, TProperty> SelectorFunc) : IPropertyBuilderOutput<TProperty>
    where TSettings : class, ISettings, new()
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
}
