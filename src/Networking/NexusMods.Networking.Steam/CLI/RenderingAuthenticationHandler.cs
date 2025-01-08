using System.Diagnostics;
using NexusMods.Abstractions.Cli;
using NexusMods.Abstractions.Steam;
using NexusMods.ProxyConsole.Abstractions;
using QRCoder;

namespace NexusMods.Networking.Steam.CLI;

public class RenderingAuthenticationHandler(IRenderer renderer): IAuthInterventionHandler
{
    public void ShowQRCode(Uri uri, CancellationToken token)
    {
        _ = Task.Run(async () =>
            {
                await renderer.RenderAsync(Renderable.Text("Please scan the QR code with your Steam Mobile App to continue the authentication process."));
                using var qrGenerator = new QRCodeGenerator();
                using var qrCodeData = qrGenerator.CreateQrCode(uri.ToString(), QRCodeGenerator.ECCLevel.L);
                using var qrCode = new AsciiQRCode(qrCodeData);
                var asciiArt = qrCode.GetGraphic(1, drawQuietZones: false);
                var lines = $"\n\n{asciiArt}\n\n";
                await renderer.RenderAsync(Renderable.Text(lines));
            }
        );
    }
}
