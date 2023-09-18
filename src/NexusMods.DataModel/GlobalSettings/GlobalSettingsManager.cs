﻿using Microsoft.Extensions.Logging;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Abstractions.Ids;

namespace NexusMods.DataModel.GlobalSettings;

/// <summary>
/// A manager for global settings that affect the application as a whole.
/// </summary>
public class GlobalSettingsManager
{
    private readonly ILogger<GlobalSettingsManager> _logger;
    private readonly IDataStore _dataStore;

    /// <summary>
    /// DI Constructor
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="dataStore"></param>
    public GlobalSettingsManager(ILogger<GlobalSettingsManager> logger,  IDataStore dataStore)
    {
        _logger = logger;
        _dataStore = dataStore;
    }

    /// <summary>
    /// Returns whether the user has opted in to metrics collection. If not set or set to false, the user has not opted in.
    /// </summary>
    /// <returns></returns>
    public bool GetMetricsOptIn()
    {
        var metricsOptIn = _dataStore.GetRaw(Ids.MetricsOptIn);
        if (metricsOptIn is null || metricsOptIn.Length == 0)
        {
            _logger.LogDebug("MetricsOptIn is null, returning false");
            return false;
        }

        if (metricsOptIn[0] == 1)
        {
            _logger.LogDebug("MetricsOptIn is true");
            return true;
        }

        _logger.LogDebug("MetricsOptIn is false");
        return false;
    }

    /// <summary>
    /// Sets whether the user has opted in to metrics collection.
    /// </summary>
    /// <param name="value"></param>
    public void SetMetricsOptIn(bool value)
    {
        _logger.LogDebug("Setting MetricsOptIn to {Value}", value);
        _dataStore.PutRaw(Ids.MetricsOptIn, new [] { value ? (byte) 1 : (byte) 0 });
    }

    /// <summary>
    /// Returns whether the user has made a decision on metrics collection. If not set, the user has not made a decision.
    /// </summary>
    /// <returns></returns>
    public bool IsMetricsOptInSet()
    {
        return _dataStore.GetRaw(Ids.MetricsOptIn) != null;
    }
}
