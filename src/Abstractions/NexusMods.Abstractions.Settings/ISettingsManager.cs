using JetBrains.Annotations;

namespace NexusMods.Abstractions.Settings;

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
    T Get<T>() where T : class, ISettings, new();

    /// <summary>
    /// Gets an observable stream to be notified about changes to <typeparamref name="T"/>.
    /// </summary>
    IObservable<T> GetChanges<T>() where T : class, ISettings, new();
}
