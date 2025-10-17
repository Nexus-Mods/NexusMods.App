using JetBrains.Annotations;

namespace NexusMods.Sdk.Tracking;

/// <summary>
/// Convenience class with global state, don't tell your parents.
/// </summary>
[PublicAPI]
public static class Tracker
{
    public static IEventTracker? EventTracker { get; private set; }
    public static IExceptionTracker? ExceptionTracker { get; private set; }

    public static void SetTracker(IEventTracker? tracker)
    {
        if (tracker is not null && EventTracker is not null) throw new InvalidOperationException("Event tracker has already been set");
        EventTracker = tracker;
    }

    public static void SetTracker(IExceptionTracker? tracker)
    {
        if (tracker is not null && ExceptionTracker is not null) throw new InvalidOperationException("Exception tracker has already been set");
        ExceptionTracker = tracker;
    }

    /// <summary>
    /// Track an exception.
    /// </summary>
    public static void TrackException(Exception exception) => ExceptionTracker?.Track(exception);

    /// <summary>
    /// Track an event.
    /// </summary>
    public static void TrackEvent<T0, T1>(EventString name, (EventString name, T0? value) property0) => EventTracker?.Track(name, property0);

    /// <summary>
    /// Track an event.
    /// </summary>
    public static void TrackEvent<T0, T1>(
        EventString name,
        (EventString name, T0? value) property0,
        (EventString name, T1? value) property1) => EventTracker?.Track(name, property0, property1);
    
    /// <summary>
    /// Track an event.
    /// </summary>
    public static void TrackEvent<T0, T1, T2>(
        EventString name,
        (EventString name, T0? value) property0,
        (EventString name, T1? value) property1,
        (EventString name, T2? value) property2) => EventTracker?.Track(name, property0, property1, property2);
    
    /// <summary>
    /// Track an event.
    /// </summary>
    public static void TrackEvent<T0, T1, T2, T3>(
        EventString name,
        (EventString name, T0? value) property0,
        (EventString name, T1? value) property1,
        (EventString name, T2? value) property2,
        (EventString name, T3? value) property3) => EventTracker?.Track(name, property0, property1, property2, property3);
    
    /// <summary>
    /// Track an event.
    /// </summary>
    public static void TrackEvent<T0, T1, T2, T3, T4>(
        EventString name,
        (EventString name, T0? value) property0,
        (EventString name, T1? value) property1,
        (EventString name, T2? value) property2,
        (EventString name, T3? value) property3,
        (EventString name, T4? value) property4) => EventTracker?.Track(name, property0, property1, property2, property3, property4);
    
    /// <summary>
    /// Track an event.
    /// </summary>
    public static void TrackEvent<T0, T1, T2, T3, T4, T5>(
        EventString name,
        (EventString name, T0? value) property0,
        (EventString name, T1? value) property1,
        (EventString name, T2? value) property2,
        (EventString name, T3? value) property3,
        (EventString name, T4? value) property4,
        (EventString name, T5? value) property5) => EventTracker?.Track(name, property0, property1, property2, property3, property4, property5);
    
    /// <summary>
    /// Track an event.
    /// </summary>
    public static void TrackEvent<T0, T1, T2, T3, T4, T5, T6>(
        EventString name,
        (EventString name, T0? value) property0,
        (EventString name, T1? value) property1,
        (EventString name, T2? value) property2,
        (EventString name, T3? value) property3,
        (EventString name, T4? value) property4,
        (EventString name, T5? value) property5,
        (EventString name, T6? value) property6) => EventTracker?.Track(name, property0, property1, property2, property3, property4, property5, property6);
    
    /// <summary>
    /// Track an event.
    /// </summary>
    public static void TrackEvent<T0, T1, T2, T3, T4, T5, T6, T7>(
        EventString name,
        (EventString name, T0? value) property0,
        (EventString name, T1? value) property1,
        (EventString name, T2? value) property2,
        (EventString name, T3? value) property3,
        (EventString name, T4? value) property4,
        (EventString name, T5? value) property5,
        (EventString name, T6? value) property6,
        (EventString name, T7? value) property7) => EventTracker?.Track(name, property0, property1, property2, property3, property4, property5, property6, property7);
    
    /// <summary>
    /// Track an event.
    /// </summary>
    public static void TrackEvent<T0, T1, T2, T3, T4, T5, T6, T7, T8>(
        EventString name,
        (EventString name, T0? value) property0,
        (EventString name, T1? value) property1,
        (EventString name, T2? value) property2,
        (EventString name, T3? value) property3,
        (EventString name, T4? value) property4,
        (EventString name, T5? value) property5,
        (EventString name, T6? value) property6,
        (EventString name, T7? value) property7,
        (EventString name, T8? value) property8) => EventTracker?.Track(name, property0, property1, property2, property3, property4, property5, property6, property7, property8);
    

    /// <summary>
    /// Track an event.
    /// </summary>
    public static void TrackEvent<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(
        EventString name,
        (EventString name, T0? value) property0,
        (EventString name, T1? value) property1,
        (EventString name, T2? value) property2,
        (EventString name, T3? value) property3,
        (EventString name, T4? value) property4,
        (EventString name, T5? value) property5,
        (EventString name, T6? value) property6,
        (EventString name, T7? value) property7,
        (EventString name, T8? value) property8,
        (EventString name, T9? value) property9) => EventTracker?.Track(name, property0, property1, property2, property3, property4, property5, property6, property7, property8, property9);

    /// <summary>
    /// Track an event.
    /// </summary>
    public static void TrackEvent(EventString name, params ReadOnlySpan<(EventString name, object? value)> properties) => EventTracker?.Track(name, properties);
}
