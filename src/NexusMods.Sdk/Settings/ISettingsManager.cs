using System.Collections.Frozen;
using JetBrains.Annotations;
using R3;

namespace NexusMods.Sdk.Settings;

/// <summary>
/// Represents a settings manager.
/// </summary>
[PublicAPI]
public interface ISettingsManager
{
    /// <summary>
    /// Sets the current value of <typeparamref name="T"/>.
    /// </summary>
    void Set<T>(T value) where T : class, ISettings, new();

    /// <summary>
    /// Gets the current value for <typeparamref name="T"/>.
    /// </summary>
    /// <returns>The current value.</returns>
    T Get<T>() where T : class, ISettings, new();

    /// <summary>
    /// Gets the default value for <typeparamref name="T"/>.
    /// </summary>
    T GetDefault<T>() where T : class, ISettings, new ();

    /// <summary>
    /// Allows you to update the current value of <typeparamref name="T"/>.
    /// </summary>
    /// <param name="updater">
    /// Lambda for updating the current value of <typeparamref name="T"/>.
    /// Only the return value of this will be saved. Modifications to the input
    /// value will be ignored, unless the modified input value gets returned.
    /// </param>
    /// <returns>The updated value.</returns>
    T Update<T>(Func<T, T> updater) where T : class, ISettings, new();

    /// <summary>
    /// Gets an observable stream to be notified about changes to <typeparamref name="T"/>.
    /// </summary>
    Observable<T> GetChanges<T>(bool prependCurrent) where T : class, ISettings, new();

    /// <summary>
    /// Gets configs for all registered settings.
    /// </summary>
    FrozenDictionary<Type, SettingsConfig> Configs { get; }
}
