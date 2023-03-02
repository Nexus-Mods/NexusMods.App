using System.Runtime.Versioning;

namespace NexusMods.Common.ProtocolRegistration;

/// <summary>
/// Protocol registration for Linux.
/// </summary>
[SupportedOSPlatform("linux")]
public class ProtocolRegistrationLinux : IProtocolRegistration
{
    /// <inheritdoc/>
    public Task<string> RegisterSelf(string protocol)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public Task<string> Register(string protocol, string friendlyName, string? commandLine = null)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public Task<bool> IsSelfHandler(string protocol)
    {
        throw new NotImplementedException();
    }
}
