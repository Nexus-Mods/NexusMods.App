namespace NexusMods.CrossPlatform.ProtocolRegistration;

/// <summary>
/// Abstracts OS-specific protocol registration logic.
/// </summary>
public interface IProtocolRegistration
{
    /// <summary>
    /// Registers the App as a protocol handler for <paramref name="uriScheme"/>.
    /// </summary>
    Task RegisterHandler(string uriScheme, bool setAsDefaultHandler = true, CancellationToken cancellationToken = default);
}
