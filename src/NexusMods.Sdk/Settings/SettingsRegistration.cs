using JetBrains.Annotations;

namespace NexusMods.Sdk.Settings;

[PublicAPI]
public record SettingsRegistration(
    Type ObjectType,
    ISettings DefaultValue,
    Func<ISettingsBuilder, ISettingsBuilder> ConfigureLambda
);
