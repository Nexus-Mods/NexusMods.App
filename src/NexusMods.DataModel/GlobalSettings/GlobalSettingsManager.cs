using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Serialization;

namespace NexusMods.DataModel.GlobalSettings;

/// <summary>
/// A manager for global settings that affect the application as a whole.
/// </summary>
public class GlobalSettingsManager
{
    private readonly ILogger<GlobalSettingsManager> _logger;
    private readonly IDataStore _dataStore;
    private readonly IAppConfigManager _appConfigManager;

    /// <summary>
    /// DI Constructor
    /// </summary>
    public GlobalSettingsManager(
        ILogger<GlobalSettingsManager> logger,
        IDataStore dataStore,
        IAppConfigManager appConfigManager)
    {
        _logger = logger;
        _dataStore = dataStore;
        _appConfigManager = appConfigManager;
    }

    /// <summary>
    /// Returns whether the user has opted in to metrics collection. If not set or set to false, the user has not opted in.
    /// </summary>
    /// <returns></returns>
    public bool GetMetricsOptIn() => _appConfigManager.GetMetricsOptIn();

    /// <summary>
    /// Sets whether the user has opted in to metrics collection.
    /// </summary>
    /// <param name="value"></param>
    public void SetMetricsOptIn(bool value) => _appConfigManager.SetMetricsOptIn(value);

    /// <summary>
    /// Returns whether the user has made a decision on metrics collection. If not set, the user has not made a decision.
    /// </summary>
    /// <returns></returns>
    public bool IsMetricsOptInSet() => _appConfigManager.IsMetricsOptInSet();
}

/// <summary>
/// Interface for the app config manager.
/// </summary>
[PublicAPI]
public interface IAppConfigManager
{
    /// <inheritdoc cref="GlobalSettingsManager.GetMetricsOptIn"/>
    public bool GetMetricsOptIn();
    /// <inheritdoc cref="GlobalSettingsManager.SetMetricsOptIn"/>
    public void SetMetricsOptIn(bool value);
    /// <inheritdoc cref="GlobalSettingsManager.IsMetricsOptInSet"/>
    public bool IsMetricsOptInSet();
}
