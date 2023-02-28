using Microsoft.Extensions.Logging;
using Moq;
using NexusMods.Common;
using System.Net;
using NexusMods.DataModel.Interprocess;
using NexusMods.DataModel.Abstractions;
using FluentAssertions;
using Moq.Protected;
using System.Text.Json;

namespace NexusMods.Networking.NexusWebApi.Tests;

public class OAuth2MessageFactoryTests
{
    private OAuth2MessageFactory _factory;
    private OAuth _auth;
    private Mock<IDataStore> _store;
    private Mock<HttpMessageHandler> _handler;

    public OAuth2MessageFactoryTests(ILogger<OAuth> logger, IMessageConsumer<NXMUrlMessage> consumer)
    {
        _handler = new Mock<HttpMessageHandler>();
        HttpClient httpClient = new HttpClient(_handler.Object);
        var idGen = new Mock<IIDGenerator>();
        var os = new Mock<IOSInterop>();
        _store = new Mock<IDataStore>();

        _auth = new OAuth(logger.As<ILogger<OAuth>>(), httpClient, idGen.Object, os.Object, consumer);
        _factory = new OAuth2MessageFactory(logger.As<ILogger<OAuth2MessageFactory>>(), _store.Object, _auth);
    }

    [Fact()]
    public async void AddsHeaderToRequest()
    {
        _store.Setup(_ => _.Get<JWTTokenEntity>(JWTTokenEntity.StoreId, false)).Returns(() => new JWTTokenEntity
        {
            AccessToken = "access_token",
            RefreshToken = "refresh_token",
            Store = _store.Object
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
        _store.Setup(_ => _.Get<JWTTokenEntity>(JWTTokenEntity.StoreId, false)).Returns(() => new JWTTokenEntity
        {
            AccessToken = "access_token",
            RefreshToken = "refresh_token",
            Store = _store.Object
        });

        _handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(RefreshToken)),
            });


        var msg = new HttpRequestMessage(HttpMethod.Get, "test://foobar");
        var ex = new HttpRequestException("Token has expired", null, HttpStatusCode.Unauthorized);
        var res = await _factory.HandleError(msg, ex, CancellationToken.None);
        res.Should().NotBeNull();
        res.Headers.Authorization!.Should().NotBeNull();
        res!.Headers.Authorization!.ToString().Should().Be("Bearer refreshed_access_token");
    }

    private JwtTokenReply RefreshToken
    {
        get
        {
            return new JwtTokenReply
            {
                AccessToken = "refreshed_access_token",
                RefreshToken = "refresh_token",
                Scope = "public",
                Type = "Bearer",
                CreatedAt = 1677143380,
                ExpiresIn = 21600,
            };
        }
    }
}