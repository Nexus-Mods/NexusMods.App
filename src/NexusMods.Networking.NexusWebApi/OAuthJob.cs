using System.Security.Cryptography;
using System.Text;
using DynamicData.Kernel;
using Microsoft.AspNetCore.WebUtilities;
using NexusMods.Abstractions.NexusWebApi;
using NexusMods.Abstractions.NexusWebApi.DTOs.OAuth;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.Networking.NexusWebApi.Auth;
using NexusMods.Sdk;
using NexusMods.Sdk.Jobs;
using R3;

namespace NexusMods.Networking.NexusWebApi;

internal sealed class OAuthJob : IOAuthJob, IJobDefinitionWithStart<OAuthJob, Optional<JwtTokenReply>>
{
    private readonly IIDGenerator _idGenerator;
    private readonly IOSInterop _os;
    private readonly HttpClient _httpClient;
    private readonly Observable<NXMOAuthUrl> _nxmUrlMessages;

    public BehaviorSubject<Uri?> LoginUriSubject { get; } = new(initialValue: null);

    private OAuthJob(
        IIDGenerator idGenerator,
        IOSInterop os,
        HttpClient httpClient,
        Observable<NXMOAuthUrl> nxmUrlMessages)
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
        Observable<NXMOAuthUrl> nxmUrlMessages)
    {
        var job = new OAuthJob(idGenerator, os, httpClient, nxmUrlMessages);
        return jobMonitor.Begin<OAuthJob, Optional<JwtTokenReply>>(job);
    }

    public async ValueTask<Optional<JwtTokenReply>> StartAsync(IJobContext<OAuthJob> context)
    {
        // see https://www.rfc-editor.org/rfc/rfc7636#section-4.1
        var codeVerifier = Convert.ToBase64String(Encoding.UTF8.GetBytes(_idGenerator.UUIDv4()));

        // see https://www.rfc-editor.org/rfc/rfc7636#section-4.2
        var codeChallengeBytes = SHA256.HashData(Encoding.UTF8.GetBytes(codeVerifier));
        var codeChallenge = WebEncoders.Base64UrlEncode(codeChallengeBytes);

        var state = _idGenerator.UUIDv4();
        var uri = OAuth.GenerateAuthorizeUrl(codeChallenge, state);
        LoginUriSubject.OnNext(uri);

        var cts = CancellationTokenSource.CreateLinkedTokenSource(context.CancellationToken);

        // Start listening first, otherwise we might miss the message
        var codeTask = _nxmUrlMessages
            .Where(state, static (oauth, state) => oauth.State == state)
            .Select(url => url.OAuth.Code)
            .WhereNotNull()
            .FirstAsync(cts.Token);

        // see https://www.rfc-editor.org/rfc/rfc7636#section-4.3
        _os.OpenUri(uri);

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
