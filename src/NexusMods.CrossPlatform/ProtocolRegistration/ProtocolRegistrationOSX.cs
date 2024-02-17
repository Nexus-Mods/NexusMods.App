namespace NexusMods.CrossPlatform.ProtocolRegistration;

/// <summary>
/// Protocol registration of OSX
/// </summary>
public class ProtocolRegistrationOSX : IProtocolRegistration
{
    /// <inheritdoc />
    public Task<string?> RegisterSelf(string protocol)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public Task<string?> Register(string protocol, string friendlyName, string workingDirectory, string commandLine)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public Task<bool> IsSelfHandler(string protocol)
    {
        throw new NotImplementedException();
    }
}
