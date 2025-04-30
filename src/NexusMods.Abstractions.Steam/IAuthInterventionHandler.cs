namespace NexusMods.Abstractions.Steam;

/// <summary>
/// A user intervention handler that can be used to request authorization information from the user.
/// </summary>
public interface IAuthInterventionHandler
{
    /// <summary>
    /// Display a QR code to the user for the given uri. When the token is cancelled, the QR code should be hidden.
    /// </summary>
    public void ShowQRCode(Uri uri, CancellationToken token);
}
