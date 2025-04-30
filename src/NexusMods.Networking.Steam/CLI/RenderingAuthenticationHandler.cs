using System.Diagnostics;
using NexusMods.Abstractions.Cli;
using NexusMods.Abstractions.Steam;
using NexusMods.ProxyConsole.Abstractions;
using QRCoder;

namespace NexusMods.Networking.Steam.CLI;

public class RenderingAuthenticationHandler: IAuthInterventionHandler
{
    /// <summary>
    /// This is a bit of a hack, but it's about the only way to get the scoped renderer into the handler.
    /// </summary>
    public static IRenderer? Renderer { get; set; } = null;

    public void ShowQRCode(Uri uri, CancellationToken token)
    {
        if (Renderer == null)
            throw new InvalidOperationException("Renderer is not set.");
        
        _ = Task.Run(async () =>
            {
                await Renderer.RenderAsync(Renderable.Text("Please scan the QR code with your Steam Mobile App to continue the authentication process."));
                using var qrGenerator = new QRCodeGenerator();
                using var qrCodeData = qrGenerator.CreateQrCode(uri.ToString(), QRCodeGenerator.ECCLevel.L);
                using var qrCode = new AsciiQRCode(qrCodeData);
                var asciiArt = qrCode.GetGraphic(1, drawQuietZones: false);
                var lines = $"\n\n{asciiArt}\n\n";
                await Renderer.RenderAsync(Renderable.Text(lines));
            }
        );
    }
}
