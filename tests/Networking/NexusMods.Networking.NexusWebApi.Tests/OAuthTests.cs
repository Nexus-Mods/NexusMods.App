using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;
using FluentAssertions;
using NexusMods.Abstractions.Activities;
using NexusMods.Abstractions.Messaging;
using NexusMods.Abstractions.NexusWebApi.DTOs.OAuth;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.CrossPlatform.Process;
using NexusMods.Networking.NexusWebApi.NMA;
using NexusMods.Networking.NexusWebApi.NMA.Messages;
using NSubstitute;

namespace NexusMods.Networking.NexusWebApi.Tests;

public class OAuthTests(
    ILogger<OAuth> logger,
    IMessageProducer<NXMUrlMessage> producer,
    IMessageConsumer<NXMUrlMessage> consumer,
    IActivityFactory activityFactory)
{
    // ReSharper disable once ContextualLoggerProblem

    [Fact]
    public async void AuthorizeRequestTest()
    {
        #region Setup
        var stateId = "00000000-0000-0000-0000-000000000000";
        var messageHandler = Substitute.ForPartsOf<MockHttpMessageHandler>();
        messageHandler
            .SendMock(Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(ReplyToken)),
            }));

        var httpClient = new HttpClient(messageHandler);

        var os = Substitute.For<IOSInterop>();
        #endregion

        #region Execution
        var oauth = new OAuth(logger, httpClient, os, consumer, activityFactory);
        var tokenTask = oauth.AuthorizeRequest(CancellationToken.None);

        await producer.Write(new NXMUrlMessage { Value = NXMUrl.Parse($"nxm://oauth/callback?state={stateId}&code=code") }, CancellationToken.None);
        var result = await tokenTask;
        #endregion

        #region Verification
        _ = os.Received(1).OpenUrl(ExpectedAuthURL, Arg.Any<CancellationToken>());
        result.Should().BeEquivalentTo(ReplyToken);
        #endregion
    }

    [Fact]
    public async void RefreshTokenTest()
    {
        #region Setup
        var messageHandler = Substitute.ForPartsOf<MockHttpMessageHandler>();
        messageHandler
            .SendMock(Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(ReplyToken)),
            }));

        var httpClient = new HttpClient(messageHandler);

        var os = Substitute.For<IOSInterop>();
        #endregion

        #region Execution
        var oauth = new OAuth(logger, httpClient, os, consumer, activityFactory);
        var token = await oauth.RefreshToken("refresh_token", CancellationToken.None);
        #endregion

        #region Verification
        _ = os.DidNotReceive().OpenUrl(Arg.Any<Uri>(), Arg.Any<CancellationToken>());
        token.Should().BeEquivalentTo(ReplyToken);
        #endregion
    }

    [Fact]
    public async void ThrowsOnInvalidResponse()
    {
        #region Setup
        var stateId = "00000000-0000-0000-0000-000000000000";
        var messageHandler = Substitute.ForPartsOf<MockHttpMessageHandler>();
        messageHandler
            .SendMock(Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("foo"),
            }));

        var httpClient = new HttpClient(messageHandler);

        var os = Substitute.For<IOSInterop>();
        #endregion

        #region Execution
        var oauth = new OAuth(logger, httpClient, os, consumer, activityFactory);
        Func<Task> call = () => oauth.AuthorizeRequest(CancellationToken.None);
        var tokenTask = call.Should().ThrowAsync<JsonException>();

        await producer.Write(new NXMUrlMessage { Value = NXMUrl.Parse($"nxm://oauth/callback?state={stateId}&code=code") }, CancellationToken.None);
        await tokenTask;
        #endregion
    }

    [Fact]
    public async void AuthorizationCanBeCanceled()
    {
        #region Setup
        var messageHandler = Substitute.ForPartsOf<MockHttpMessageHandler>();
        var httpClient = new HttpClient(messageHandler);

        var os = Substitute.For<IOSInterop>();
        var cts = new CancellationTokenSource();
        #endregion

        #region Execution
        var oauth = new OAuth(logger, httpClient, os, consumer, activityFactory);
        Func<Task> call = () => oauth.AuthorizeRequest(cts.Token);
        var task = call.Should().ThrowAsync<OperationCanceledException>();
        cts.Cancel();
        await task;
        #endregion
    }

    private static readonly JwtTokenReply ReplyToken =
        new()
        {
            AccessToken = "access_token",
            RefreshToken = "refresh_token",
            Scope = "public",
            Type = "Bearer",
            CreatedAt = 1677143380,
            ExpiresIn = 21600,
        };

    // ReSharper disable once InconsistentNaming
    private readonly Uri ExpectedAuthURL = new("https://users.nexusmods.com/oauth/authorize?response_type=code&scope=openid profile email&code_challenge_method=S256&client_id=nma&redirect_uri=nxm%3A%2F%2Foauth%2Fcallback&code_challenge=QMZ4D7BLeehAXINE9NZ8dho2i5AYVTbfqJ8PhQ4eUrE&state=00000000-0000-0000-0000-000000000000");

    [Fact]
    public void Test_GenerateAuthorizeUrl()
    {
        var res = OAuth.GenerateAuthorizeUrl("QMZ4D7BLeehAXINE9NZ8dho2i5AYVTbfqJ8PhQ4eUrE", "00000000-0000-0000-0000-000000000000");
        res.Should().Be(ExpectedAuthURL);
    }
}
