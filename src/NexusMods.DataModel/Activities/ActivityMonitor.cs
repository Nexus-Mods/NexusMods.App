using System.Collections.ObjectModel;
using DynamicData;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Activities;

namespace NexusMods.DataModel.Activities;

public class ActivityMonitor : IActivityFactory, IActivityMonitor
{
    private readonly ILogger<ActivityMonitor> _logger;
    private SourceCache<Activity, ActivityId> _activities = new(x => x.Id);

    /// <summary>
    /// DI constructor.
    /// </summary>
    public ActivityMonitor(ILogger<ActivityMonitor> logger)
    {
        _logger = logger;
        _activities.Connect()
            .Transform(x => (IReadOnlyActivity) x)
            .Bind(out _activitiesCasted);
    }

    /// <inheritdoc />
    public IActivitySource Create(string template, params object[] arguments)
    {
        var activity = new Activity(this);
        activity.SetStatusMessage(template, arguments);
        _activities.AddOrUpdate(activity);
        return activity;
    }


    public IActivitySource<T> Create<T>(string template, params object[] arguments)
    {
        throw new NotImplementedException();
    }


    internal void Remove(Activity activity)
    {
        _activities.Remove(activity);
    }

    private readonly ReadOnlyObservableCollection<IReadOnlyActivity> _activitiesCasted;

    /// <inheritdoc />
    public ReadOnlyObservableCollection<IReadOnlyActivity> Activities => _activitiesCasted;
}
