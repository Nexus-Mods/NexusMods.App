namespace NexusMods.Sdk.Settings;


/// <summary>
/// Identifies a setting by its type and an optional scope key.
/// </summary>
public readonly record struct SettingKey(Type SettingType, string? Key);
