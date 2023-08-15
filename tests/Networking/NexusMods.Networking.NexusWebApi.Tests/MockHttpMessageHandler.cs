using JetBrains.Annotations;

namespace NexusMods.Networking.NexusWebApi.Tests;

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
