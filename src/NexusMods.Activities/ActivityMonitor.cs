

using System.Collections.ObjectModel;
using System.Numerics;
using DynamicData;
using NexusMods.Abstractions.Activities;

namespace NexusMods.Activities;

internal class ActivityMonitor : IActivityFactory, IActivityMonitor
{
    private readonly SourceCache<Activity, ActivityId> _activities = new(x => x.Id);

    /// <summary>
    /// DI constructor.
    /// </summary>
    public ActivityMonitor()
    {
        _activities.Connect()
            .Transform(x => (IReadOnlyActivity) x)
            .Bind(out _activitiesCasted);
    }

    /// <inheritdoc />
    public IActivitySource Create(ActivityGroup group, string template, params object[] arguments)
    {
        var activity = new Activity(this, group, null);
        activity.SetStatusMessage(template, arguments);
        _activities.AddOrUpdate(activity);
        return activity;
    }


    /// <inheritdoc />
    public IActivitySource CreateWithPayload(ActivityGroup group, object payload, string template, params object[] arguments)
    {
        var activity = new Activity(this, group, payload);
        activity.SetStatusMessage(template, arguments);
        _activities.AddOrUpdate(activity);
        return activity;
    }


    /// <inheritdoc />
    public IActivitySource<T> Create<T>(ActivityGroup group, string template, params object[] arguments)
        where T : struct, IDivisionOperators<T, T, double>, IAdditionOperators<T, T, T>, IDivisionOperators<T, double, T>
    {
        var activity = new Activity<T>(this, group, null);
        activity.SetStatusMessage(template, arguments);
        _activities.AddOrUpdate(activity);
        return activity;
    }


    internal void Remove(Activity activity)
    {
        _activities.Remove(activity);
    }

    private readonly ReadOnlyObservableCollection<IReadOnlyActivity> _activitiesCasted;

    /// <inheritdoc />
    public ReadOnlyObservableCollection<IReadOnlyActivity> Activities => _activitiesCasted;
}
