using System.Diagnostics;
using JetBrains.Annotations;

namespace NexusMods.Telemetry;

/// <summary>
/// Provides metadata to an event.
/// </summary>
[PublicAPI]
public readonly struct EventMetadata
{
    /// <summary>
    /// Current time.
    /// </summary>
    public readonly TimeOnly CurrentTime;

    /// <summary>
    /// Name of the event.
    /// </summary>
    public readonly string? Name;

    /// <summary>
    /// Constructor.
    /// </summary>
    [Obsolete(error: true, message: "Don't use the default constructor!")]
    public EventMetadata()
    {
        throw new UnreachableException();
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    public EventMetadata(string? name, TimeProvider? timeProvider = null)
    {
        Name = name;
        CurrentTime = TimeOnly.FromDateTime((timeProvider ?? TimeProvider.System).GetLocalNow().LocalDateTime);
    }

    /// <summary>
    /// Checks whether the struct wasn't default initialized.
    /// </summary>
    public bool IsValid() => Name is not null || CurrentTime != default(TimeOnly);
}
