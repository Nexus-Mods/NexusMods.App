namespace NexusMods.Abstractions.Settings;

internal record SettingsOverrideInformation(
    Type Type,
    Func<object, object> OverrideMethod
);
