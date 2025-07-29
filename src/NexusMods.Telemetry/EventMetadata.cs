using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Numerics;
using System.Text;
using DynamicData.Kernel;
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
    /// Value of the event.
    /// </summary>
    public readonly Optional<double> Value;

    /// <summary>
    /// Constructor.
    /// </summary>
    [Obsolete(error: true, message: "Don't use the default constructor!")]
    public EventMetadata()
    {
        throw new UnreachableException();
    }

    /// <summary>
    /// Create event metadata with a value.
    /// </summary>
    public static EventMetadata Create<T>(string? name, T value, TimeProvider? timeProvider = null) where T : INumber<T>, IConvertible
    {
        var doubleValue = value.ToDouble(CultureInfo.InvariantCulture);
        return new EventMetadata(name, value: doubleValue, timeProvider: timeProvider);
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    public EventMetadata(string? name, TimeProvider? timeProvider = null) : this(name, value: Optional<double>.None, timeProvider: timeProvider) { }

    private EventMetadata(string? name, Optional<double> value = default, TimeProvider? timeProvider = null)
    {
        Name = name;
        Value = value;
        CurrentTime = TimeOnly.FromDateTime((timeProvider ?? TimeProvider.System).GetLocalNow().DateTime);
    }

    /// <summary>
    /// Checks whether the struct wasn't default initialized.
    /// </summary>
    public bool IsValid() => Name is not null || CurrentTime != default(TimeOnly);

    internal byte[] SafeName => Name is null ? [] : EncodeString(Name);
    internal byte[] SafeValue => Value.HasValue ? EncodeValue(Value.Value) : [];

    private static byte[] EncodeString(string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        return WebUtility.UrlEncodeToBytes(bytes, offset: 0, count: bytes.Length);
    }

    internal static string FormatValue(double value)
    {
        var integralPart = Math.Truncate(value);
        if (Math.Abs(integralPart - value) < double.Epsilon)
        {
            var integer = (int)integralPart;
            return integer.ToString(CultureInfo.InvariantCulture);
        }

        return value.ToString("F3", CultureInfo.InvariantCulture);
    }

    private static byte[] EncodeValue(double value)
    {
        return EncodeString(FormatValue(value));
    }
}
