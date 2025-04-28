using JetBrains.Annotations;

namespace NexusMods.Abstractions.NexusWebApi.Types;

/// <summary>
/// NXM protocol registration test.
/// </summary>
[PublicAPI]
public class NXMProtocolRegistrationCheck : NXMUrl
{
    /// <summary>
    /// Unique ID of the test.
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    /// Constructor.
    /// </summary>
    public NXMProtocolRegistrationCheck(Uri uri)
    {
        var parsedQuery = System.Web.HttpUtility.ParseQueryString(uri.Query);
        var rawId = parsedQuery["id"] ?? throw new ArgumentException($"invalid nxm url \"{uri}\"");
        Id = Guid.Parse(rawId);
    }

    /// <inheritdoc/>
    public override string ToString() => $"nxm://protocol-test/?id={Id}";
}
