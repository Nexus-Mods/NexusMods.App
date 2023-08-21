using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Abstractions.Ids;

namespace NexusMods.DataModel.GlobalSettings;

/// <summary>
/// Ids for the GlobalSettings category.
/// </summary>
public static class Ids
{
    /// <summary>
    /// Signals whether the user has opted in to metrics collection. If not set or set to false, the user has not opted in.
    /// </summary>
    public static Id64 MetricsOptIn = new(EntityCategory.GlobalSettings, 0x1);
}
