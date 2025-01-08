using System.Diagnostics;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Steam;
using QRCoder;

namespace NexusMods.Networking.Steam;

public class LoggingAuthInterventionHandler(ILogger<LoggingAuthInterventionHandler> logger) : IAuthInterventionHandler
{
    public void ShowQRCode(Uri uri, CancellationToken token)
    {
        logger.LogInformation("Please scan this QR code with the Steam app on your phone.");

        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(uri.ToString(), QRCodeGenerator.ECCLevel.L);
        using var qrCode = new AsciiQRCode(qrCodeData);
        var asciiArt = qrCode.GetGraphic(1, drawQuietZones: false);
        var lines = $"\n\n{asciiArt}\n\n";
        logger.LogInformation(lines);
        Debug.WriteLine(lines);
    }
}
