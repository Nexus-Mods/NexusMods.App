using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Security.Cryptography;
using System.Text;
using DynamicData.Kernel;
using NexusMods.Abstractions.Jobs;
using NexusMods.Abstractions.NexusWebApi;
using NexusMods.Abstractions.NexusWebApi.DTOs.OAuth;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.CrossPlatform.Process;
using NexusMods.Extensions.BCL;
using NexusMods.Networking.NexusWebApi.Auth;

namespace NexusMods.Networking.NexusWebApi;

internal sealed class OAuthJob : IOAuthJob, IJobDefinitionWithStart<OAuthJob, Optional<JwtTokenReply>>
{
    private readonly IIDGenerator _idGenerator;
    private readonly IOSInterop _os;
    private readonly HttpClient _httpClient;
    private readonly Subject<NXMOAuthUrl> _nxmUrlMessages;

    public R3.Subject<Uri> LoginUriSubject { get; } = new();

    private OAuthJob(
        IIDGenerator idGenerator,
        IOSInterop os,
        HttpClient httpClient,
        Subject<NXMOAuthUrl> nxmUrlMessages)
    {
        _idGenerator = idGenerator;
        _os = os;
        _httpClient = httpClient;
        _nxmUrlMessages = nxmUrlMessages;
    }

    public static IJobTask<OAuthJob, Optional<JwtTokenReply>> Create(
        IJobMonitor jobMonitor,
        IIDGenerator idGenerator,
        IOSInterop os,
        HttpClient httpClient,
        Subject<NXMOAuthUrl> nxmUrlMessages)
    {
        var job = new OAuthJob(idGenerator, os, httpClient, nxmUrlMessages);
        return jobMonitor.Begin<OAuthJob, Optional<JwtTokenReply>>(job);
    }

    public async ValueTask<Optional<JwtTokenReply>> StartAsync(IJobContext<OAuthJob> context)
    {
        // see https://www.rfc-editor.org/rfc/rfc7636#section-4.1
        var codeVerifier = _idGenerator.UUIDv4().ToBase64();

        // see https://www.rfc-editor.org/rfc/rfc7636#section-4.2
        var codeChallengeBytes = SHA256.HashData(Encoding.UTF8.GetBytes(codeVerifier));
        var codeChallenge = StringBase64Extensions.Base64UrlEncode(codeChallengeBytes);

        var state = _idGenerator.UUIDv4();
        var uri = OAuth.GenerateAuthorizeUrl(codeChallenge, state);
        LoginUriSubject.OnNext(uri);

        var cts = CancellationTokenSource.CreateLinkedTokenSource(context.CancellationToken);

        // Start listening first, otherwise we might miss the message
        var codeTask = _nxmUrlMessages
            .Where(oauth => oauth.State == state)
            .Select(url => url.OAuth.Code)
            .Where(code => code is not null)
            .Select(code => code!)
            .ToAsyncEnumerable()
            .FirstAsync(cts.Token);

        // see https://www.rfc-editor.org/rfc/rfc7636#section-4.3
        await _os.OpenUrl(uri, cancellationToken: context.CancellationToken);

        cts.CancelAfter(TimeSpan.FromMinutes(3));
        var code = await codeTask;

        var token = await OAuth.AuthorizeToken(codeVerifier, code, _httpClient, context.CancellationToken);
        return token;
    }

    public void Dispose()
    {
        LoginUriSubject.Dispose();
    }
}
