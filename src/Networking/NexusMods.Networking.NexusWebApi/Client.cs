using Microsoft.Extensions.Logging;

namespace NexusMods.Networking.NexusWebApi;

public class Client
{
    private readonly ILogger<Client> _logger;
    private readonly IHttpMessageFactory _factory;

    public Client(ILogger<Client> logger, IHttpMessageFactory factory)
    {
        _logger = logger;
        _factory = factory;
    }

    public async Task Verify()
    {
        
    }
}