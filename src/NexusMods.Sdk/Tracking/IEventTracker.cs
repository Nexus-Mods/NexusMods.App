using JetBrains.Annotations;

namespace NexusMods.Sdk.Tracking;

/// <summary>
/// Tracker for events.
/// </summary>
[PublicAPI]
public interface IEventTracker
{
    /// <summary>
    /// Tracks an event.
    /// </summary>
    void Track<T0>(EventString name, (EventString name, T0? value) property0);

    /// <summary>
    /// Tracks an event.
    /// </summary>
    void Track<T0, T1>(EventString name,
        (EventString name, T0? value) property0,
        (EventString name, T1? value) property1);

    /// <summary>
    /// Tracks an event.
    /// </summary>
    void Track<T0, T1, T2>(EventString name,
        (EventString name, T0? value) property0,
        (EventString name, T1? value) property1,
        (EventString name, T2? value) property2);

    /// <summary>
    /// Tracks an event.
    /// </summary>
    void Track<T0, T1, T2, T3>(EventString name,
        (EventString name, T0? value) property0,
        (EventString name, T1? value) property1,
        (EventString name, T2? value) property2,
        (EventString name, T3? value) property3);

    /// <summary>
    /// Tracks an event.
    /// </summary>
    void Track<T0, T1, T2, T3, T4>(EventString name,
        (EventString name, T0? value) property0,
        (EventString name, T1? value) property1,
        (EventString name, T2? value) property2,
        (EventString name, T3? value) property3,
        (EventString name, T4? value) property4);

    /// <summary>
    /// Tracks an event.
    /// </summary>
    void Track<T0, T1, T2, T3, T4, T5>(EventString name,
        (EventString name, T0? value) property0,
        (EventString name, T1? value) property1,
        (EventString name, T2? value) property2,
        (EventString name, T3? value) property3,
        (EventString name, T4? value) property4,
        (EventString name, T5? value) property5);

    /// <summary>
    /// Tracks an event.
    /// </summary>
    void Track<T0, T1, T2, T3, T4, T5, T6>(EventString name,
        (EventString name, T0? value) property0,
        (EventString name, T1? value) property1,
        (EventString name, T2? value) property2,
        (EventString name, T3? value) property3,
        (EventString name, T4? value) property4,
        (EventString name, T5? value) property5,
        (EventString name, T6? value) property6);

    /// <summary>
    /// Tracks an event.
    /// </summary>
    void Track<T0, T1, T2, T3, T4, T5, T6, T7>(EventString name,
        (EventString name, T0? value) property0,
        (EventString name, T1? value) property1,
        (EventString name, T2? value) property2,
        (EventString name, T3? value) property3,
        (EventString name, T4? value) property4,
        (EventString name, T5? value) property5,
        (EventString name, T6? value) property6,
        (EventString name, T7? value) property7);

    /// <summary>
    /// Tracks an event.
    /// </summary>
    void Track<T0, T1, T2, T3, T4, T5, T6, T7, T8>(EventString name,
        (EventString name, T0? value) property0,
        (EventString name, T1? value) property1,
        (EventString name, T2? value) property2,
        (EventString name, T3? value) property3,
        (EventString name, T4? value) property4,
        (EventString name, T5? value) property5,
        (EventString name, T6? value) property6,
        (EventString name, T7? value) property7,
        (EventString name, T8? value) property8);

    /// <summary>
    /// Tracks an event.
    /// </summary>
    void Track<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(EventString name,
        (EventString name, T0? value) property0,
        (EventString name, T1? value) property1,
        (EventString name, T2? value) property2,
        (EventString name, T3? value) property3,
        (EventString name, T4? value) property4,
        (EventString name, T5? value) property5,
        (EventString name, T6? value) property6,
        (EventString name, T7? value) property7,
        (EventString name, T8? value) property8,
        (EventString name, T9? value) property9);

    /// <summary>
    /// Tracks an event.
    /// </summary>
    void Track(EventString name, params ReadOnlySpan<(EventString name, object? value)> properties);
}
