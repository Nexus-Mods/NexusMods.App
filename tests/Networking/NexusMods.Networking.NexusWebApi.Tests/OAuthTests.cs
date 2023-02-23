using Xunit;
using NexusMods.Networking.NexusWebApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NexusMods.Common;
using NexusMods.DataModel.Interprocess;
using Moq;
using System.Net;
using Moq.Protected;
using NexusMods.Networking.NexusWebApi.Types;
using NexusMods.DataModel.JsonConverters;
using System.Text.Json;
using FluentAssertions;
using System.Data.Entity.Core.Metadata.Edm;

namespace NexusMods.Networking.NexusWebApi.Tests;

public class DelegatingHandlerStub : DelegatingHandler
{
    private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handlerFunc;
    public DelegatingHandlerStub()
    {
        _handlerFunc = (request, cancellationToken) =>
        {
            var resp = new HttpResponseMessage(HttpStatusCode.OK);
            resp.RequestMessage = request;
            return Task.FromResult(resp);
        };
    }

    public DelegatingHandlerStub(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handlerFunc)
    {
        _handlerFunc = handlerFunc;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return _handlerFunc(request, cancellationToken);
    }
}

public class OAuthTests
{
    private readonly ILogger<OAuth> _logger;
    private readonly IOSInterop _os;
    private readonly IMessageProducer<NXMUrlMessage> _producer;
    private readonly IMessageConsumer<NXMUrlMessage> _consumer;
    public OAuthTests(ILogger<OAuth> logger, IOSInterop os, IMessageProducer<NXMUrlMessage> producer, IMessageConsumer<NXMUrlMessage> consumer)
    {
        _logger = logger;
        _os = os;
        _producer = producer;
        _consumer = consumer;
    }

    [Fact()]
    public async void AuthorizeRequestTest()
    {
        #region Setup
        var stateId = "00000000-0000-0000-0000-000000000000";

        var messageHandler = new Mock<HttpMessageHandler>();
        messageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(ReplyToken)),
            });
        HttpClient httpClient = new HttpClient(messageHandler.Object);

        var idGen = new Mock<IIDGenerator>();
        idGen.Setup(_ => _.UUIDv4()).Returns(stateId);

        var os = new Mock<IOSInterop>();
        #endregion

        #region Execution
        var oauth = new OAuth(_logger, httpClient, idGen.Object, os.Object, _consumer);
        var tokenTask = oauth.AuthorizeRequest(CancellationToken.None);

        await _producer.Write(new NXMUrlMessage { Value = NXMUrl.Parse($"nxm://oauth/callback?state={stateId}&code=code") }, CancellationToken.None);
        var result = await tokenTask;
        #endregion

        #region Verification
        idGen.Verify(_ => _.UUIDv4(), Times.Exactly(2));
        os.Verify(_ => _.OpenURL(ExpectedAuthURL), Times.Once);
        result.Should().BeEquivalentTo(ReplyToken);
        #endregion
    }

    [Fact()]
    public async void RefreshTokenTest()
    {
        #region Setup
        var stateId = "00000000-0000-0000-0000-000000000000";

        var messageHandler = new Mock<HttpMessageHandler>();
        messageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(ReplyToken)),
            });
        HttpClient httpClient = new HttpClient(messageHandler.Object);

        var idGen = new Mock<IIDGenerator>();
        idGen.Setup(_ => _.UUIDv4()).Returns(stateId);

        var os = new Mock<IOSInterop>();
        #endregion

        #region Execution
        var oauth = new OAuth(_logger, httpClient, idGen.Object, os.Object, _consumer);
        var token = await oauth.RefreshToken("refresh_token", CancellationToken.None);
        #endregion

        #region Verification
        idGen.Verify(_ => _.UUIDv4(), Times.Never);
        os.Verify(_ => _.OpenURL(It.IsAny<string>()), Times.Never);
        token.Should().BeEquivalentTo(ReplyToken);
        #endregion
    }

    private JwtTokenReply ReplyToken
    {
        get
        {
            return new JwtTokenReply
            {
                AccessToken = "access_token",
                RefreshToken = "refresh_token",
                Scope = "public",
                Type = "Bearer",
                CreatedAt = 1677143380,
                ExpiresIn = 21600,
            };
        }
    }

    private string ExpectedAuthURL
    {
        get
        {
            return "https://users.nexusmods.com/oauth/authorize?response_type=code&scope=public&code_challenge_method=S256&client_id=vortex&redirect_uri=nxm%3A%2F%2Foauth%2Fcallback&code_challenge=-pSOp5xdZffKD0gc1lb5JALgN_ZtE9X573ib3yS8BT4&state=00000000-0000-0000-0000-000000000000";
        }
    }
}