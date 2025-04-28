namespace NexusMods.Abstractions.Settings;

internal record SettingsTypeInformation(
    Type ObjectType,
    ISettings DefaultValue,
    Func<ISettingsBuilder, ISettingsBuilder> ConfigureLambda
);
