namespace NexusMods.Common.ProtocolRegistration;

/// <summary>
/// Protocol registration logic of Mac OSX
/// </summary>
public class ProtocolRegistrationOSX : IProtocolRegistration
{
    public Task<string?> RegisterSelf(string protocol)
    {
        throw new NotImplementedException();
    }

    public Task<string?> Register(string protocol, string friendlyName, string commandLine)
    {
        throw new NotImplementedException();
    }

    public Task<bool> IsSelfHandler(string protocol)
    {
        throw new NotImplementedException();
    }
}
