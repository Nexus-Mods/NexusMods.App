using JetBrains.Annotations;

namespace NexusMods.Sdk.Settings;

[PublicAPI]
public record OverrideHack(Type SettingsType, Func<object, object> Method, string? Key);
