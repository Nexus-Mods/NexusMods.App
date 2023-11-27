using NexusMods.Abstractions.Activities;
using NexusMods.Abstractions.Values;

namespace NexusMods.DataModel.Activities;

/// <summary>
/// An activity is a long running code process that can be monitored and (optionally) cancelled. These are useful for
/// providing progress feedback to the user to show that something is happening, instead of just a static screen or
/// some sort of spinning indicator. When the activity source is disposed, the activity is considered complete and will
/// be removed from the activity monitor. This interface is just for the producer size of the activity, consumers will
/// receive an readonly interface to the activity.
/// </summary>
public interface IActivitySource : IDisposable
{

    /// <summary>
    /// Updates the status message for the activity, keep parameters in their native format so that the message
    /// can be formatted in the UI with links, etc.
    /// </summary>
    /// <param name="template"></param>
    /// <param name="arguments"></param>
    /// <returns></returns>
    public void SetStatusMessage(string template, params object[] arguments);

    /// <summary>
    /// Sets the progress of the activity, to a specific value. May not return instantly
    /// if the activity is throttled or has been paused.
    /// </summary>
    /// <param name="percent"></param>
    /// <param name="token"></param>
    public ValueTask SetProgress(Percent percent, CancellationToken token = default);

    /// <summary>
    /// Adds to the progress of the activity, by a specific value. May not return instantly
    /// if the activity is throttled or has been paused.
    /// </summary>
    /// <param name="percent"></param>
    /// <param name="token"></param>
    public ValueTask AddProgress(Percent percent, CancellationToken token = default);
}

/// <summary>
/// An activity that manages a progress value of type <typeparamref name="T"/>. Updating this value will update the
/// progress percentage of the activity.
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IActivitySource<in T> : IActivitySource
{
    /// <summary>
    /// Sets the maximum value of the progress. If this is null, the progress will be considered indeterminate.
    /// </summary>
    /// <param name="max"></param>
    public void SetMax(T? max);

    /// <summary>
    /// Sets the progress of the activity, to a specific value.
    /// </summary>
    /// <param name="value"></param>
    public void SetProgress(T value);

    /// <summary>
    /// Adds to the progress of the activity, by a specific value.
    /// </summary>
    /// <param name="value"></param>
    public void AddProgress(T value);
}
