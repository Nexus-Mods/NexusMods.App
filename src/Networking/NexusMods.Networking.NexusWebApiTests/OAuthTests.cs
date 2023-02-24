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
    private readonly IMessageConsumer<NXMUrlMessage> _message;
    public OAuthTests(ILogger<OAuth> logger, IOSInterop os, IMessageConsumer<NXMUrlMessage> message)
    {
        _logger = logger;
        _os = os;
        _message = message;
    }

    [Fact()]
    public async void AuthorizeRequestTest()
    {
        var messageHandler = new Mock<HttpMessageHandler>();

        messageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{'name':thecodebuzz,'city':'USA'}"),
            });

        HttpClient httpClient = new HttpClient(messageHandler.Object);

        var oauth = new OAuth(_logger, httpClient, _os, _message);
        var token = await oauth.AuthorizeRequest(CancellationToken.None);

        Assert.NotNull(token);
    }

    [Fact()]
    public void RefreshTokenTest()
    {
        Assert.True(false, "This test needs an implementation");
    }
}