using JetBrains.Annotations;

namespace NexusMods.Sdk.Settings;

[PublicAPI]
public record SettingsConfig(
    Type Type,
    Func<IServiceProvider, object> DefaultValueFactory,
    PropertyConfig[] Properties,
    StorageBackendOptions? StorageBackendOptions
);

[PublicAPI]
public record PropertyConfig(
    PropertyOptionsWrapper Options,
    IContainerOptions? ContainerOptions,
    Func<ISettingsManager, object> GetValue,
    Func<ISettingsManager, object> GetDefaultValue,
    Action<ISettingsManager, object> Update
)
{
    public T GetValueCasted<T>(ISettingsManager settingsManager) => Cast<T>(GetValue(settingsManager));
    public T GetDefaultValueCasted<T>(ISettingsManager settingsManager) => Cast<T>(GetDefaultValue(settingsManager));

    private static T Cast<T>(object obj)
    {
        if (obj is not T value) throw new InvalidOperationException($"Expected value to be of type `{typeof(T)}` but found {obj.GetType()}");
        return value;
    }
}

[PublicAPI]
public class PropertyOptionsWrapper : PropertyOptions
{
    /// <inheritdoc cref="PropertyOptions{TSettings,TProperty}.DescriptionFactory"/>
    public required Func<object, string> DescriptionFactory { get; init; }

    /// <inheritdoc cref="PropertyOptions{TSettings,TProperty}.Validation"/>
    public Func<object, ValidationResult>? Validation { get; init; }
}
