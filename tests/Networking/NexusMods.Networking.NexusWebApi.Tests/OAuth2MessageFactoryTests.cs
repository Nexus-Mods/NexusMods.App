using Microsoft.Extensions.Logging;
using NexusMods.Common;
using System.Net;
using NexusMods.DataModel.Interprocess;
using NexusMods.DataModel.Abstractions;
using FluentAssertions;
using System.Text.Json;
using NexusMods.Common.OSInterop;
using NexusMods.DataModel.Interprocess.Jobs;
using NexusMods.Networking.NexusWebApi.DTOs.OAuth;
using NexusMods.Networking.NexusWebApi.NMA;
using NexusMods.Networking.NexusWebApi.NMA.Messages;
using NSubstitute;

namespace NexusMods.Networking.NexusWebApi.Tests;

public class OAuth2MessageFactoryTests
{
    private readonly OAuth2MessageFactory _factory;
    private readonly IDataStore _store;
    private readonly MockHttpMessageHandler _handler;

    public OAuth2MessageFactoryTests(
        ILoggerFactory loggerFactory,
        IMessageConsumer<NXMUrlMessage> consumer,
        IInterprocessJobManager jobManager)
    {
        _store = Substitute.For<IDataStore>();

        _handler = Substitute.ForPartsOf<MockHttpMessageHandler>();
        var httpClient = new HttpClient(_handler);

        var idGen = Substitute.For<IIDGenerator>();
        var os = Substitute.For<IOSInterop>();

        var auth = new OAuth(loggerFactory.CreateLogger<OAuth>(), httpClient, idGen, os, consumer, jobManager);
        _factory = new OAuth2MessageFactory(_store, auth);
    }

    [Fact]
    public async void AddsHeaderToRequest()
    {
        _store
            .Get<JWTTokenEntity>(JWTTokenEntity.StoreId, canCache: false)
            .Returns(_ => new JWTTokenEntity
            {
                AccessToken = "access_token",
                RefreshToken = "refresh_token"
            });

        var request = await _factory.Create(HttpMethod.Get, new Uri("test://foobar"));

        request.Headers.Authorization.Should().NotBeNull();
        request.Headers.Authorization!.ToString().Should().Be("Bearer access_token");
    }

    [Fact()]
    public async void ThrowsIfMissingToken()
    {
        Func<Task<HttpRequestMessage>> func = async () => await _factory.Create(HttpMethod.Get, new Uri("test://foobar"));

        await func.Should().ThrowAsync<Exception>();
    }

    [Fact()]
    public async void ForwardsUnrelatedErrors()
    {
        var msg = new HttpRequestMessage(HttpMethod.Get, "test://foobar");
        var ex = new HttpRequestException();
        var res = await _factory.HandleError(msg, ex, CancellationToken.None);
        res.Should().BeNull();
    }

    [Fact()]
    public async void RequestsRefreshOnTokenExpired()
    {
        _store
            .Get<JWTTokenEntity>(JWTTokenEntity.StoreId, canCache: false)
            .Returns(_ => new JWTTokenEntity
            {
                AccessToken = "access_token",
                RefreshToken = "refresh_token"
            });

        _handler
            .SendMock(Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(RefreshToken))
            }));

        var msg = new HttpRequestMessage(HttpMethod.Get, "test://foobar");
        var ex = new HttpRequestException("Token has expired", null, HttpStatusCode.Unauthorized);
        var res = await _factory.HandleError(msg, ex, CancellationToken.None);
        res.Should().NotBeNull();
        res!.Headers.Authorization!.Should().NotBeNull();
        res.Headers.Authorization!.ToString().Should().Be("Bearer refreshed_access_token");
    }

    private static readonly JwtTokenReply RefreshToken =
        new()
        {
            AccessToken = "refreshed_access_token",
            RefreshToken = "refresh_token",
            Scope = "public",
            Type = "Bearer",
            CreatedAt = 1677143380,
            ExpiresIn = 21600,
        };
}
