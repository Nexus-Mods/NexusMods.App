namespace NexusMods.CrossPlatform.ProtocolRegistration;

/// <summary>
/// Protocol registration of OSX.
/// </summary>
public class ProtocolRegistrationOSX : IProtocolRegistration
{
    /// <inheritdoc/>
    public Task RegisterHandler(string uriScheme, bool setAsDefaultHandler = true, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
