using System.Net;
using System.Text;
using FluentAssertions;
using JetBrains.Annotations;
using Microsoft.Extensions.Time.Testing;
using NexusMods.Abstractions.NexusWebApi;
using NexusMods.Abstractions.NexusWebApi.Types;
using NSubstitute;
using Xunit;

namespace NexusMods.Telemetry.Tests;

public class EventSenderTests
{
    [Fact]
    public async Task Test()
    {
        var loginManager = Substitute.For<ILoginManager>();
        loginManager.UserInfo.Returns(new UserInfo
        {
            UserId = UserId.From(1337),
            Name = "",
            AvatarUrl = null,
            IsPremium = false,
        });

        var messageHandler = Substitute.ForPartsOf<MockHttpMessageHandler>();
        messageHandler
            .SendMock(Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("okay", Encoding.UTF8),
            }))
            .AndDoes(callInfo =>
            {
                var requestMessage = callInfo.ArgAt<HttpRequestMessage>(position: 0);
                var content = requestMessage.Content;
                content.Should().NotBeNull();

                using var stream = content!.ReadAsStream();
                using var textReader = new StreamReader(stream, Encoding.UTF8);
                var res = textReader.ReadToEnd();
                ExpectJson("""{ "requests": ["?idsite=7&rec=1&apiv=1&send_image=0&ca=1&uid=1337&e_c=Game&e_a=Add+Game&e_n=Mount+%26+Blade&h=0&m=0&s=0","?idsite=7&rec=1&apiv=1&send_image=0&ca=1&uid=1337&e_c=Loadout&e_a=Create+Loadout&e_n=Mount+%26+Blade&h=0&m=0&s=1"] }""", res);
            });

        var sender = new EventSender(loginManager, new HttpClient(messageHandler));

        var timeProvider = new FakeTimeProvider();
        sender.AddEvent(definition: Events.Game.AddGame, metadata: new EventMetadata(name: "Mount & Blade", timeProvider: timeProvider));

        timeProvider.Advance(delta: TimeSpan.FromSeconds(1));
        sender.AddEvent(definition: Events.Loadout.CreateLoadout, metadata: new EventMetadata(name: "Mount & Blade", timeProvider: timeProvider));

        await sender.Run();
    }

    private static void ExpectJson([LanguageInjection(InjectedLanguage.JSON)] string expected, string actual)
    {
        actual.Should().Be(expected);
    }

    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public class MockHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return SendMock(request, cancellationToken);
        }

        public virtual Task<HttpResponseMessage> SendMock(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
