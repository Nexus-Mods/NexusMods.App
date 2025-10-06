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
    void Track<T0>(EventDefinition e, (string name, T0 value) property0);

    /// <summary>
    /// Tracks an event.
    /// </summary>
    void Track<T0, T1>(EventDefinition e,
        (string name, T0 value) property0,
        (string name, T1 value) property1);

    /// <summary>
    /// Tracks an event.
    /// </summary>
    void Track<T0, T1, T2>(EventDefinition e,
        (string name, T0 value) property0,
        (string name, T1 value) property1,
        (string name, T2 value) property2);

    /// <summary>
    /// Tracks an event.
    /// </summary>
    void Track<T0, T1, T2, T3>(EventDefinition e,
        (string name, T0 value) property0,
        (string name, T1 value) property1,
        (string name, T2 value) property2,
        (string name, T3 value) property3);

    /// <summary>
    /// Tracks an event.
    /// </summary>
    void Track<T0, T1, T2, T3, T4>(EventDefinition e,
        (string name, T0 value) property0,
        (string name, T1 value) property1,
        (string name, T2 value) property2,
        (string name, T3 value) property3,
        (string name, T4 value) property4);

    /// <summary>
    /// Tracks an event.
    /// </summary>
    void Track<T0, T1, T2, T3, T4, T5>(EventDefinition e,
        (string name, T0 value) property0,
        (string name, T1 value) property1,
        (string name, T2 value) property2,
        (string name, T3 value) property3,
        (string name, T4 value) property4,
        (string name, T5 value) property5);

    /// <summary>
    /// Tracks an event.
    /// </summary>
    void Track<T0, T1, T2, T3, T4, T5, T6>(EventDefinition e,
        (string name, T0 value) property0,
        (string name, T1 value) property1,
        (string name, T2 value) property2,
        (string name, T3 value) property3,
        (string name, T4 value) property4,
        (string name, T5 value) property5,
        (string name, T6 value) property6);

    /// <summary>
    /// Tracks an event.
    /// </summary>
    void Track<T0, T1, T2, T3, T4, T5, T6, T7>(EventDefinition e,
        (string name, T0 value) property0,
        (string name, T1 value) property1,
        (string name, T2 value) property2,
        (string name, T3 value) property3,
        (string name, T4 value) property4,
        (string name, T5 value) property5,
        (string name, T6 value) property6,
        (string name, T7 value) property7);

    /// <summary>
    /// Tracks an event.
    /// </summary>
    void Track<T0, T1, T2, T3, T4, T5, T6, T7, T8>(EventDefinition e,
        (string name, T0 value) property0,
        (string name, T1 value) property1,
        (string name, T2 value) property2,
        (string name, T3 value) property3,
        (string name, T4 value) property4,
        (string name, T5 value) property5,
        (string name, T6 value) property6,
        (string name, T7 value) property7,
        (string name, T8 value) property8);
    
    /// <summary>
    /// Tracks an event.
    /// </summary>
    void Track<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(EventDefinition e,
        (string name, T0 value) property0,
        (string name, T1 value) property1,
        (string name, T2 value) property2,
        (string name, T3 value) property3,
        (string name, T4 value) property4,
        (string name, T5 value) property5,
        (string name, T6 value) property6,
        (string name, T7 value) property7,
        (string name, T8 value) property8,
        (string name, T9 value) property9);

    /// <summary>
    /// Tracks an event.
    /// </summary>
    void Track(EventDefinition e, params ReadOnlySpan<(string name, object value)> properties);
}
