using DynamicData.Kernel;
using JetBrains.Annotations;

namespace NexusMods.Sdk.Tracking;

/// <summary>
/// Convenience class with global state, don't tell your parents.
/// </summary>
[PublicAPI]
public static class Tracker
{
    private static IEventTracker? _eventTracker;
    private static IExceptionTracker? _exceptionTracker;

    public static void SetTracker(IEventTracker? tracker)
    {
        if (tracker is not null && _eventTracker is not null) throw new InvalidOperationException("Event tracker has already been set");
        _eventTracker = tracker;
    }

    public static void SetTracker(IExceptionTracker? tracker)
    {
        if (tracker is not null && _exceptionTracker is not null) throw new InvalidOperationException("Exception tracker has already been set");
        _exceptionTracker = tracker;
    }

    /// <summary>
    /// Track an exception.
    /// </summary>
    public static void TrackException(Exception exception) => _exceptionTracker?.Track(exception);

    /// <summary>
    /// Track an event.
    /// </summary>
    public static void TrackEvent<T0, T1>(EventDefinition e, (string name, T0? value) property0) => _eventTracker?.Track(e, property0);

    /// <summary>
    /// Track an event.
    /// </summary>
    public static void TrackEvent<T0, T1>(
        EventDefinition e,
        (string name, T0? value) property0,
        (string name, T1? value) property1) => _eventTracker?.Track(e, property0, property1);
    
    /// <summary>
    /// Track an event.
    /// </summary>
    public static void TrackEvent<T0, T1, T2>(
        EventDefinition e,
        (string name, T0? value) property0,
        (string name, T1? value) property1,
        (string name, T2? value) property2) => _eventTracker?.Track(e, property0, property1, property2);
    
    /// <summary>
    /// Track an event.
    /// </summary>
    public static void TrackEvent<T0, T1, T2, T3>(
        EventDefinition e,
        (string name, T0? value) property0,
        (string name, T1? value) property1,
        (string name, T2? value) property2,
        (string name, T3? value) property3) => _eventTracker?.Track(e, property0, property1, property2, property3);
    
    /// <summary>
    /// Track an event.
    /// </summary>
    public static void TrackEvent<T0, T1, T2, T3, T4>(
        EventDefinition e,
        (string name, T0? value) property0,
        (string name, T1? value) property1,
        (string name, T2? value) property2,
        (string name, T3? value) property3,
        (string name, T4? value) property4) => _eventTracker?.Track(e, property0, property1, property2, property3, property4);
    
    /// <summary>
    /// Track an event.
    /// </summary>
    public static void TrackEvent<T0, T1, T2, T3, T4, T5>(
        EventDefinition e,
        (string name, T0? value) property0,
        (string name, T1? value) property1,
        (string name, T2? value) property2,
        (string name, T3? value) property3,
        (string name, T4? value) property4,
        (string name, T5? value) property5) => _eventTracker?.Track(e, property0, property1, property2, property3, property4, property5);
    
    /// <summary>
    /// Track an event.
    /// </summary>
    public static void TrackEvent<T0, T1, T2, T3, T4, T5, T6>(
        EventDefinition e,
        (string name, T0? value) property0,
        (string name, T1? value) property1,
        (string name, T2? value) property2,
        (string name, T3? value) property3,
        (string name, T4? value) property4,
        (string name, T5? value) property5,
        (string name, T6? value) property6) => _eventTracker?.Track(e, property0, property1, property2, property3, property4, property5, property6);
    
    /// <summary>
    /// Track an event.
    /// </summary>
    public static void TrackEvent<T0, T1, T2, T3, T4, T5, T6, T7>(
        EventDefinition e,
        (string name, T0? value) property0,
        (string name, T1? value) property1,
        (string name, T2? value) property2,
        (string name, T3? value) property3,
        (string name, T4? value) property4,
        (string name, T5? value) property5,
        (string name, T6? value) property6,
        (string name, T7? value) property7) => _eventTracker?.Track(e, property0, property1, property2, property3, property4, property5, property6, property7);
    
    /// <summary>
    /// Track an event.
    /// </summary>
    public static void TrackEvent<T0, T1, T2, T3, T4, T5, T6, T7, T8>(
        EventDefinition e,
        (string name, T0? value) property0,
        (string name, T1? value) property1,
        (string name, T2? value) property2,
        (string name, T3? value) property3,
        (string name, T4? value) property4,
        (string name, T5? value) property5,
        (string name, T6? value) property6,
        (string name, T7? value) property7,
        (string name, T8? value) property8) => _eventTracker?.Track(e, property0, property1, property2, property3, property4, property5, property6, property7, property8);
    

    /// <summary>
    /// Track an event.
    /// </summary>
    public static void TrackEvent<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(
        EventDefinition e,
        (string name, T0? value) property0,
        (string name, T1? value) property1,
        (string name, T2? value) property2,
        (string name, T3? value) property3,
        (string name, T4? value) property4,
        (string name, T5? value) property5,
        (string name, T6? value) property6,
        (string name, T7? value) property7,
        (string name, T8? value) property8,
        (string name, T9? value) property9) => _eventTracker?.Track(e, property0, property1, property2, property3, property4, property5, property6, property7, property8, property9);

    /// <summary>
    /// Track an event.
    /// </summary>
    public static void TrackEvent(EventDefinition e, params ReadOnlySpan<(string name, object? value)> properties) => _eventTracker?.Track(e, properties);
}
