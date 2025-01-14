using System.Net;
using System.Text;
using JetBrains.Annotations;

namespace NexusMods.Telemetry;

/// <summary>
/// Defines an event.
/// </summary>
/// <param name="Category">The event category</param>
/// <param name="Action">The event action</param>
[PublicAPI]
public record EventDefinition(string Category, string Action)
{
    internal byte[] SafeCategory { get; } = Encode(Category);
    internal byte[] SafeAction { get; } = Encode(Action);

    private static byte[] Encode(string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        return WebUtility.UrlEncodeToBytes(bytes, offset: 0, count: bytes.Length);
    }
};
