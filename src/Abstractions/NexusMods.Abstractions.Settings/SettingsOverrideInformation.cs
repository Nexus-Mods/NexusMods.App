namespace NexusMods.Abstractions.Settings;

internal record SettingsOverrideInformation(
    Type Type,
    Func<IServiceProvider, object, object> OverrideMethod
);
