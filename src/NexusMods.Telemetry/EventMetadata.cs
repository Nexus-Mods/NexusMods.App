using System.Diagnostics;
using System.Net;
using System.Text;
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
        CurrentTime = TimeOnly.FromDateTime((timeProvider ?? TimeProvider.System).GetLocalNow().DateTime);
    }

    /// <summary>
    /// Checks whether the struct wasn't default initialized.
    /// </summary>
    public bool IsValid() => Name is not null || CurrentTime != default(TimeOnly);

    internal byte[] SafeName => Name is null ? [] : Encode(Name);

    private static byte[] Encode(string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        return WebUtility.UrlEncodeToBytes(bytes, offset: 0, count: bytes.Length);
    }
}
