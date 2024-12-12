using System.Collections.Concurrent;
using QRCoder;
using SteamKit2;
using SteamKit2.Authentication;
using SteamKit2.CDN;

namespace NexusMods.Stores.Steam;

public class Session
{
    private readonly SteamConfiguration _clientConfiguration;
    private readonly SteamClient _steamClient;
    private readonly CallbackManager _callbacks;
    private readonly SteamUser _steamUser;
    private readonly SteamApps _steamApps;
    
    private ConcurrentBag<SteamApps.LicenseListCallback.License> _licenses = [];
    
    private bool _running = false;
    private readonly SteamContent _steamContent;
    private readonly Client _cdnClient;

    public Session()
    {
        _steamClient = new SteamClient();
        _steamUser = _steamClient.GetHandler<SteamUser>()!;
        _steamApps = _steamClient.GetHandler<SteamApps>()!;
        _steamContent = _steamClient.GetHandler<SteamContent>()!;
        _cdnClient = new Client(_steamClient);
        
        _callbacks = new CallbackManager(_steamClient);
        _callbacks.Subscribe<SteamClient.ConnectedCallback>(o => Task.Run(() => ConnectedCallback(o)));
        _callbacks.Subscribe<SteamClient.DisconnectedCallback>(DisconnectedCallback);
        _callbacks.Subscribe<SteamUser.LoggedOnCallback>(LoggedOnCallback);
        _callbacks.Subscribe<SteamApps.LicenseListCallback>(LicenseListCallback);
        _callbacks.Subscribe<SteamApps.PICSProductInfoCallback>(o => Task.Run(() => PICSProductInfoCallback(o)));
    }

    private Task PICSProductInfoCallback(SteamApps.PICSProductInfoCallback picsProductInfoCallback)
    {
        var result = new Dictionary<string, object>();
        foreach (var kv in picsProductInfoCallback.Apps.First().Value.KeyValues.Children)
        {
            ToJson(result, kv);
        }
        throw new NotImplementedException();
    }

    private void ToJson(Dictionary<string, object> result, KeyValue kv)
    {
        if (kv.Value != null)
            result[kv.Name!] = kv.Value;
        else
        {
            var subDict = new Dictionary<string, object>();
            foreach (var subKv in kv.Children)
            {
                ToJson(subDict, subKv);
            }
            result[kv.Name!] = subDict;
        }
    }
    

    public async Task ConnectAsync()
    {
        _running = true;
        _steamClient.Connect();
        while (_running)
        {
            await _callbacks.RunWaitCallbackAsync(CancellationToken.None);
        }
    }
    

    private void LicenseListCallback(SteamApps.LicenseListCallback obj)
    {
        foreach (var license in obj.LicenseList)
        {
            _licenses.Add(license);
        }
    }

    private void LoggedOnCallback(SteamUser.LoggedOnCallback callback)
    {
        if (callback.Result != EResult.OK)
        {
            Console.WriteLine("Unable to logon to Steam: {0} / {1}", callback.Result, callback.ExtendedResult);
            _running = false;
            return;
        }
        
        Console.WriteLine("Successfully logged on!");
        
        Console.WriteLine("Requesting license list...");
        //_steamApps.PICSGetProductInfo(new SteamApps.PICSRequest(413150), null);

        Task.Run(async () => await GetManifestInfo());
    }
    
    private async Task GetManifestInfo()
    {
        var appId = (uint)413150;
        var depotId = (uint)413151;
        var manifestId = 1364246008775303529UL;
        var requestCode = await GetDepotManifestRequestCodeAsync(depotId, appId, manifestId, "public");
        var depotKey = await GetDepotKey(depotId, appId);
        var servers = await _steamContent.GetServersForSteamPipe();
        var usable = servers
            .Where(s => s.Type == "CDN")
            .ToArray();
        Random.Shared.Shuffle(usable);
        var server = usable.First();
        var cdnAuthToken = await RequestCDNAuthTokenAsync(appId, depotId, server);

        Console.WriteLine("Got {0} servers", servers.Count);
        var manifest = await _cdnClient.DownloadManifestAsync(depotId, manifestId, requestCode, server, depotKey, cdnAuthToken: cdnAuthToken);
        
        Console.WriteLine("Got manifest with {0} files", manifest.Files.Count);
        
    }

    private async Task<string> RequestCDNAuthTokenAsync(uint appId, uint depotId, Server server)
    {
        var cdnAuth = await _steamContent.GetCDNAuthToken(appId, depotId, server.Host!);
        
        if (cdnAuth.Result != EResult.OK)
            Console.WriteLine("Failed to get CDN auth token for depot {0}", depotId);
        else
            Console.WriteLine("Got CDN auth token for depot {0}", depotId);

        return cdnAuth.Token;
    }

    private async Task<byte[]> GetDepotKey(uint depotId, uint appId)
    {
        var key = await _steamApps.GetDepotDecryptionKey(depotId, appId);
        if (key.Result != EResult.OK)
            Console.WriteLine("Failed to get depot key for depot {0}", depotId);
        else
            Console.WriteLine("Got depot key for depot {0}", depotId);

        return key!.DepotKey;
    }

    private async Task<ulong> GetDepotManifestRequestCodeAsync(uint depotId, uint appId, ulong manifestId, string branch)
    {
        var requestCode = await _steamContent.GetManifestRequestCode(depotId, appId, manifestId, branch);
        if (requestCode == 0)
            throw new Exception("Unable to get request code for depot " + depotId + " manifest " + manifestId);
        return requestCode;
    }

    private void DisconnectedCallback(SteamClient.DisconnectedCallback obj)
    {
        throw new NotImplementedException();
    }

    private async Task ConnectedCallback(SteamClient.ConnectedCallback callback)
    {
        var authSession = await _steamClient.Authentication.BeginAuthSessionViaQRAsync(new AuthSessionDetails());

        authSession.ChallengeURLChanged = () =>
        {
            Console.WriteLine();
            Console.WriteLine("Steam has generated a new QR code for you to scan");

            DrawQRCode(authSession);
        };
        
        DrawQRCode(authSession);
        var pollResponse = await authSession.PollingWaitForResultAsync();
        
        Console.WriteLine("Logging in as " + pollResponse.AccountName + "...");
        _steamUser.LogOn(new SteamUser.LogOnDetails
            {
                Username = pollResponse.AccountName,
                AccessToken = pollResponse.RefreshToken,
            }
        );

    }

    private void DrawQRCode(QrAuthSession authSession)
    {
        Console.WriteLine( $"Challenge URL: {authSession.ChallengeURL}" );
        Console.WriteLine();

        // Encode the link as a QR code
        using var qrGenerator = new QRCodeGenerator();
        var qrCodeData = qrGenerator.CreateQrCode( authSession.ChallengeURL, QRCodeGenerator.ECCLevel.L );
        using var qrCode = new AsciiQRCode( qrCodeData );
        var qrCodeAsAsciiArt = qrCode.GetGraphic( 1, drawQuietZones: false );

        Console.WriteLine( "Use the Steam Mobile App to sign in via QR code:" );
        Console.WriteLine( qrCodeAsAsciiArt );
    }

    public SteamUser.LogOnDetails LogOnDetails { get; set; }
}
