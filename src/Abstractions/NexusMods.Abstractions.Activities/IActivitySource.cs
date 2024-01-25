using System.Numerics;

namespace NexusMods.Abstractions.Activities;

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
    /// Updates the StartTime and starting percentage of the activity, for activities that can be paused and resumed.
    /// </summary>
    public void StartOrResume();

    /// <summary>
    /// Updates the status message for the activity, keep parameters in their native format so that the message
    /// can be formatted in the UI with links, etc.
    /// </summary>
    /// <param name="template"></param>
    /// <param name="arguments"></param>
    /// <returns></returns>
    public void SetStatusMessage(string template, params object[] arguments);

    /// <summary>
    /// Sets the progress of the activity, to a specific value.
    /// </summary>
    /// <param name="percent"></param>
    public void SetProgress(Percent percent);

    /// <summary>
    /// Adds to the progress of the activity, by a specific value.
    /// </summary>
    /// <param name="percent"></param>
    public void AddProgress(Percent percent);
}

/// <summary>
/// An activity that manages a progress value of type <typeparamref name="T"/>. Updating this value will update the
/// progress percentage of the activity.
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IActivitySource<in T> : IActivitySource
    where T : IDivisionOperators<T, T, double>, IAdditionOperators<T, T, T>, ISubtractionOperators<T, T, T>
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

    /// <summary>
    /// Sets the StartingTime and starting value of the activity, for activities that can be paused and resumed.
    /// </summary>
    /// <param name="startingValue"></param>
    public void StartOrResume(T startingValue);

}
